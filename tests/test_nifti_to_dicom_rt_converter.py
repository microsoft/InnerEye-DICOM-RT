#  ------------------------------------------------------------------------------------------
#  Copyright (c) Microsoft Corporation. All rights reserved.
#  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
#  ------------------------------------------------------------------------------------------

"""
Tests for nifti_to_dicom_rt_converter.
"""

import logging
from pathlib import Path
from typing import List
from pydicom import dcmread

try:
    from InnerEye_DICOM_RT.nifti_to_dicom_rt_converter import echo, get_version, rtconvert  # type: ignore
except ImportError:
    logging.info("using local src")
    from src.InnerEye_DICOM_RT.nifti_to_dicom_rt_converter import echo, get_version, rtconvert  # type: ignore

logger = logging.getLogger('test_rtconvert')
logger.setLevel(logging.DEBUG)


def test_get_version() -> None:
    """
    Test that .dotnet core can be called --info and that it is
    running version 3.1.
    """
    (stdout, stderr) = get_version()

    logger.debug("stdout: %s", stdout)
    logger.debug("stderr: %s", stderr)

    assert 'Microsoft.NETCore.App 3.1.' in stdout


def test_echo() -> None:
    """
    Test that the test Echo dll can be called and returns the test string and no error.
    """
    test_string = "hello world2!"
    (stdout, stderr) = echo(test_string)

    logger.debug("stdout: %s", stdout)
    logger.debug("stderr: %s", stderr)

    assert stderr == ''
    assert stdout == test_string + '\n'


def test_echo_err() -> None:
    """
    Test that the test Echo dll can be called and returns the test and error strings.
    """
    test_string = "hello world2!"
    test_error = "Test error."
    (stdout, stderr) = echo(test_string, test_error)

    logger.debug("stdout: %s", stdout)
    logger.debug("stderr: %s", stderr)

    assert stderr == test_error + '\n'
    assert stdout == test_string + '\n'


# The directory containing this file.
THIS_DIR: Path = Path(__file__).parent.resolve()
# The TestData directory.
TEST_DATA_DIR: Path = THIS_DIR / "TestData"
# Test Nifti file.
TestNiftiSegmentationLocation: Path = TEST_DATA_DIR / "hnsegmentation.nii.gz"
# Test reference series.
TestDicomVolumeLocation: Path = TEST_DATA_DIR / "HN"
# Target test output file.
TestOutputFile: Path = THIS_DIR / "test.dcm"

# Test fill holes.
FillHoles: List[bool] = [
    True, True, True, True,
    False, False, True, True,
    True, True, False, True,
    True, True, True, False,
    True, False, True, True,
    False, True
]

# Test ROIInterpretedType.
ROIInterpretedTypes: List[str] = [
    "ORGAN", "None", "CTV", "EXTERNAL",
    "ORGAN", "None", "CTV", "EXTERNAL",
    "ORGAN", "None", "CTV", "EXTERNAL",
    "ORGAN", "None", "CTV", "EXTERNAL",
    "ORGAN", "None", "CTV", "EXTERNAL",
    "ORGAN", "None"
]

# Test structure colors.
StructureColors: List[str] = [
    "FF0001", "FF0002", "FF0003", "FF0004",
    "FF0101", "FF0102", "FF0103", "FF0103",
    "FF0201", "FF02FF", "FF0203", "FF0204",
    "FF0301", "FF0302", "01FF03", "FF0304",
    "FF0401", "00FFFF", "FF0403", "FF0404",
    "FF0501", "FF0502"
]

# Test structure names.
StructureNames: List[str] = [
    "External", "parotid_l", "parotid_r", "smg_l",
    "smg_r", "spinal_cord", "brainstem", "globe_l",
    "Globe_r", "mandible", "spc_muscle", "mpc_muscle",
    "Cochlea_l", "cochlea_r", "lens_l", "lens_r",
    "optic_chiasm", "optic_nerve_l", "optic_nerve_r", "pituitary_gland",
    "lacrimal_gland_l", "lacrimal_gland_r"
]

Manufacturer = "Contosos"
Interpreter = "Ai"
ModelId = "XYZ:12"

def test_rtconvert() -> None:
    """
    Test calling RTConvert for the test data.
    """
    (stdout, stderr) = rtconvert(
        in_file=TestNiftiSegmentationLocation,
        reference_series=TestDicomVolumeLocation,
        out_file=TestOutputFile,
        struct_names=StructureNames,
        struct_colors=StructureColors,
        fill_holes=FillHoles,
        roi_interpreted_types=ROIInterpretedTypes,
        manufacturer=Manufacturer,
        interpreter=Interpreter,
        modelId=ModelId
    )

    logger.debug("stdout: %s", stdout)
    logger.debug("stderr: %s", stderr)

    assert stderr == ''
    assert "Successfully written" in stdout

    assert TestOutputFile.is_file()

    with open(TestOutputFile, 'rb') as infile:
        ds = dcmread(infile)

        assert ds is not None

        # Check the modality
        assert ds.Modality == 'RTSTRUCT'

        assert ds.Manufacturer == Manufacturer
        assert ds.SoftwareVersions == ModelId

        assert len(ds.StructureSetROISequence) == len(StructureNames)

        for i, item in enumerate(StructureNames):
            assert ds.StructureSetROISequence[i].ROINumber == i + 1
            assert ds.StructureSetROISequence[i].ROIName == item
            assert Interpreter in ds.RTROIObservationsSequence[i].ROIInterpreter
            assert ds.RTROIObservationsSequence[i].RTROIInterpretedType == ('' if ROIInterpretedTypes[i] == 'None' else ROIInterpretedTypes[i])

        assert len(ds.ROIContourSequence) == len(StructureNames)

        for i, item in enumerate(StructureNames):
            assert ds.ROIContourSequence[i].ReferencedROINumber == i + 1
            assert ds.ROIContourSequence[i].ROIDisplayColor == _parse_rgb(StructureColors[i])

def _parse_rgb(rgb: str) -> List[int]:
    """
    Convert the string representation of RGB color to an int list
    :param rgb: Color string
    :return: List of [R, G, B] components.
    """
    return [int(rgb[i:i+2], 16) for i in (0, 2, 4)]

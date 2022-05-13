#  ------------------------------------------------------------------------------------------
#  Copyright (c) Microsoft Corporation. All rights reserved.
#  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
#  ------------------------------------------------------------------------------------------

"""
Wrapper functions for calling into .netcore dlls.
"""

import logging
import os
from pathlib import Path
import subprocess
import sys
from typing import List, Optional, Tuple
from dotnetcore2 import runtime  # type: ignore

logger = logging.getLogger('rtconvert')
logger.setLevel(logging.DEBUG)

# Name of RTConvert dll
RTCONVERT_DLL_NAME = "Microsoft.RTConvert.Console.dll"
# Name of Test Echo dll
ECHO_DLL_NAME = "Echo.dll"


def rtconvert(*, in_file: Path, reference_series: Path, out_file: Path, struct_names: List[str],
              struct_colors: List[str], fill_holes: List[bool], roi_interpreted_types: List[str],
              modelId: str, manufacturer: str, interpreter: str) -> Tuple[str, str]:
    """
    Call RTConvert dll for Nifti to DICOM-RT image conversion.
    :param in_file: Path to Nifti file.
    :param reference_series: Path to directory of reference DICOM files.
    :param out_file: Path to output DICOM-RT file.
    :param struct_names: List of names of structures.
    :param struct_colors: List of structure colors in hexadecimal format of the form "FF0080".
        If there are less colors than struct_names the remaining structs will be colored red.
    :param fill_holes: List of fill hole flags.
        If there are less bools than struct_names the remaining structs will assume false.
    :param roi_interpreted_types: List of ROIInterpretedType. Possible values (None, CTV, ORGAN, EXTERNAL).
    :param modelId: Model name and version from AzureML. E.g. Prostate:123
    :param manufacturer: Manufacturer for the DICOM-RT
    :param interpreter: Interpreter for the DICOM-RT
    """
    dll_path = _make_dll_path(RTCONVERT_DLL_NAME)

    return _wrapper([
        str(dll_path),
        "--in-file=" + str(in_file),
        "--reference-series=" + str(reference_series),
        "--out-file=" + str(out_file),
        "--struct-names=" + _make_array(struct_names),
        "--struct-colors=" + _make_array(struct_colors),
        "--fill-holes=" + _make_array(_format_bools(fill_holes)),
        "--roi-interpreted-types=" + _make_array(roi_interpreted_types),
        "--manufacturer=" + manufacturer,
        "--interpreter=" + interpreter,
        "--modelId=" + modelId,
    ])


def echo(text: str, error: Optional[str] = None) -> Tuple[str, str]:
    """
    Call Echo dll to test calling dotnet from Python.
    :param text: String to pass to Echo. Should be passed to stdout.
    :param error: Optional string to pass to Echo. If present, should be passed to stderr.
    """
    dll_path = _make_dll_path(ECHO_DLL_NAME)

    if error is None:
        return _wrapper([str(dll_path), text])

    return _wrapper([str(dll_path), text, error])


def get_version() -> Tuple[str, str]:
    """
    Return dotnet --info.
    """
    return _wrapper(['--info'])


def _make_dll_path(dll_name: str) -> Path:
    """
    Derive the expected path to a dll.
    :param dll_name: dll name.
    :return: Expected path to dll.
    """
    current_folder = Path(__file__).parent.resolve()
    return current_folder / "bin" / "netcoreapp2.1" / dll_name


def _format_bool(b: bool) -> str:
    """
    bool to string compatible with C#.
    :param b: bool to format.
    :return: stringified b.
    """
    return "true" if b else "false"


def _format_bools(bs: List[bool]) -> List[str]:
    """
    bool list to string list compatible with C#.
    :param bs: bools to format.
    :return: stringified bs.
    """
    return [_format_bool(b) for b in bs]


def _make_array(parts: List[str]) -> str:
    """
    Utility to format an array of strings for passing to command line.
    :param parts: List of strings.
    :return: Formatted string.
    """
    return "\"" + ",".join(parts) + "\""


def _wrapper(args: List[str]) -> Tuple[str, str]:
    """
    Given a list of dotnet command line arguments, create a dotnet process and pipe the
    outputs back. Return the outputs.
    :param args: List of dotnet command line arguments.
    :return: Tuple of stdout, stderr
    """

    # Ensure that any dependences for dotnet are downloaded.
    # This should only happen once.
    try:
        dependencies_path = runtime.ensure_dependencies()
    except Exception as e:
        logger.error('Failed to ensure dependencies %s', str(e))
        raise

    dotnet_path = runtime.get_runtime_path()

    engine_cmd = [dotnet_path] + args

    logger.info("engine_cmd: %s", engine_cmd)

    env = os.environ.copy()
    if dependencies_path is not None:
        if 'LD_LIBRARY_PATH' in env:
            env['LD_LIBRARY_PATH'] += ':{}'.format(dependencies_path)
        else:
            env['LD_LIBRARY_PATH'] = dependencies_path

    if sys.platform == 'win32':
        env['PATH'] = env.get('PATH') or ''
        logger.debug("env library path: %s", env['PATH'])
    else:
        # For Linux LD_LIBRARY_PATH is an environment variable containing a list of paths.
        # When the linker is linking dynamic libraries/shared libraries it will search
        # paths in LD_LIBRARY_PATH in preference to standard library paths.
        env['LD_LIBRARY_PATH'] = env.get('LD_LIBRARY_PATH') or ''
        logger.debug("env library path: %s", env['LD_LIBRARY_PATH'])

    with subprocess.Popen(
            engine_cmd,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            env=env,
            text=True) as proc:
        if proc.stdout:
            stdout = proc.stdout.read()
        if proc.stderr:
            stderr = proc.stderr.read()

    return (stdout, stderr)

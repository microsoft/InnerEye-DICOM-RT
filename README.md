# Introduction 

InnerEye-DICOM-RT contains tools to convert medical datasets in NIFTI format to DICOM-RT. Datasets converted using 
this tool can be consumed directly by [InnerEye-DeepLearning](https://github.com/microsoft/InnerEye-DeepLearning).

Most of the work is done by a .NET Core 2.1 project in RTConvert, written in C#. There is a very lightweight wrapper
around this so that it can be consumed from Python. The wrapper relies on the PyPI package https://pypi.org/project/dotnetcore2/ which wraps up .NET Core 2.1.

# Installing

## Git for Windows

Get the installer from [Git for Windows](https://git-scm.com/download/win)

 The installer will prompt you to "Select Components". Make sure that you tick 
* Git LFS (Large File Support)
* Git Credential Manager for Windows

After the installation, open a command prompt or the Git Bash:
- Run `git lfs install` to set up the hooks in git
- Run `git config --global core.autocrlf true` to ensure that line endings are working as expected

Clone the InnerEye-DICOM-RT repository on your machine: Run `git lfs clone --recursive https://github.com/microsoft/InnerEye-DICOM-RT`

## Visual Studio / .NET Core

The C# components can be built with the .NET Core SDK. We use version 2.1 for compatibility with the PyPI package `dotnetcore2`.
 Installation instructions are here: https://docs.microsoft.com/en-us/dotnet/core/install/. 
Visual Studio is not required to build, but if you wish to use it then for .Net Core 2.1 you need at least: 
[Visual Studio 2017 version 15.7](https://visualstudio.microsoft.com/vs/?utm_medium=microsoft&utm_source=docs.microsoft.com&utm_campaign=inline+link).

### RTConvert

RTConvert can be built from a .NET Core command line:

```bash
dotnet build RTConvert
```

There are unit tests:

```bash
dotnet test RTConvert
```

Note that the unit tests have a dependency on `System.Drawing` and that on Linux `System.Drawing` requires a native package:

```bash
apt-get -s install libgdiplus
```

Finally, for consumption by the Python wrapper, this solution must be published:

```bash
dotnet publish RTConvert --configuration Release -p:Platform=x64
```

This should create a folder with all the requirements for RTConvert at: 
`RTConvert/Microsoft.RTConvert.Console/bin/x64/Release/netcoreapp2.1/publish/*`

### Echo

Echo is a very simple application that takes 1 or 2 arguments. The first is echoed to `stdout`, and if a 
second argument is supplied then it is echoed to `stderr`. This is only required for units tests to establish
that a .NET Core application can be called.

Echo can be built from a .NET Core command line:

```bash
dotnet build Echo
```

There are no unit tests.

Finally, for consumption by the Python wrapper, this solution must be published:

```bash
dotnet publish Echo --configuration Release -p:Platform=x64
```

This should create a folder with all the requirements for Echo at: `Echo/Echo/bin/x64/Release/netcoreapp2.1/publish/*`

## Python

The Python wrapper is in `src/InnerEye_DICOM_RT/nifti_to_dicom_rt_converter.py`. It simply uses `subprocess.Popen` to invoke
the .NET Core application passing in the relevant dll and command line arguments.

It does require that the RTConvert and Echo published packages are copied to the folder: `src/InnerEye_DICOM_RT/bin/netcoreapp2.1`.

Note that the github build action does this automatically, but if testing then this needs to be done
manually.

The Python package is created with:

```bash
python setup.py sdist bdist_wheel
```
which builds a source distribution and wheel to the `dist` folder.

To run the Python tests:

```bash
pip install pytest dotnetcore2
pytest tests
```

## Usage

To consume this package:

```bash
pip install InnerEye-DICOM-RT
```

To call RTConvert:

```python
    from InnerEye_DICOM_RT.nifti_to_dicom_rt_converter import rtconvert

    (stdout, stderr) = rtconvert(
        in_file=NiftiSegmentationLocation,
        reference_series=DicomVolumeLocation,
        out_file=OutputFile,
        struct_names=StructureNames,
        struct_colors=StructureColors,
        fill_holes=FillHoles,
        roi_interpreted_types=ROIInterpretedTypes,
        manufacturer=Manufacturer,
        interpreter=Interpreter,
        modelId=ModelId
    )
```

where:
* `in_file` is the path to the input Nifti file. This file is a 3D volume in [Nifti format](https://nifti.nimh.nih.gov/).
* `reference_series` is the path to the input folder containing the reference DICOM series;
* `out_file` is the path to the output DICOM-RT file;
* `struct_names` is a list of structure names like: ["External", "parotid_l", "parotid_r", "smg_l"].
    Each structure name corresponds to a non-zero voxel value in the input volume. In the example External corresponds to voxel
    value 1, parotid_l to 2, etc. Voxels with value 0 are dropped.
    If there are voxels without a corresponding structure name, they will also be dropped.
    The structure name will become its ROI Name in the Structure Set ROI Sequence in the Structure Set in the DICOM-RT file.
* `struct_colors` is a list of structure colors in hexadecimal notation like: ["000000", "FF0080", "00FF00", "0000FF"].
    Each color in this list corresponds to a structure in struct_names and will become its ROI Display Color
    in the ROI Contour Sequence in the ROI Contour in the DICOM-RT file.
    If there are less colors than struct_names, or if an entry is empty, the default is red (FF0000);
* `fill_holes` is a list of bools like: [True, False, True].
    If there are less bools than struct_names, or if an entry is empty, the default is false.
    If True then any contours found per slice will have their holes filled, otherwise contours will be returned
    as found.
* `modelId` Model name and version from AzureML. E.g. Prostate:123
* `manufacturer` Manufacturer for the DICOM-RT (check DICOM-RT documentation)
* `interpreter` Interpreter for the DICOM-RT (check DICOM-RT documentation)
* `roi_interpreted_types` is a list of ROIInterpretedType. Possible values (None, CTV, ORGAN, EXTERNAL).

[MIT License](LICENSE)

**You are responsible for the performance, the necessary testing, and if needed any regulatory clearance for
 any of the models produced by this toolbox.**

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.

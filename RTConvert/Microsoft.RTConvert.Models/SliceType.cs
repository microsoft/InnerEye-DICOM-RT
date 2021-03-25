// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SliceType.cs" company="Microsoft InnerEye">
// Copyright (c) 2016 All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Microsoft.RTConvert.Models
{
    /// <summary>
    /// The slice type.
    /// More info: https://en.wikipedia.org/wiki/Anatomical_plane
    /// </summary>
    public enum SliceType
    {
        /// <summary>
        /// The axial XY plane.
        /// </summary>
        Axial,

        /// <summary>
        /// The coronal XZ plane.
        /// </summary>
        Coronal,

        /// <summary>
        /// The sagittal YZ plane.
        /// </summary>
        Sagittal,
    }
}

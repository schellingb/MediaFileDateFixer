#region License
//
// Copyright 2002-2017 Drew Noakes
// Ported from Java to C# by Yakov Danilov for Imazen LLC in 2014
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
// More information about this project is available at:
//
//    https://github.com/drewnoakes/metadata-extractor-dotnet
//    https://drewnoakes.com/code/exif/
//
#endregion

using MetadataExtractor.IO;
using MetadataExtractor.Util;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;

#if DOTNET35
using DirectoryList = System.Collections.Generic.IList<MetadataExtractor.Directory>;
#else
using DirectoryList = System.Collections.Generic.IReadOnlyList<MetadataExtractor.Directory>;
#endif

#if !METADATAEXTRACTOR_HAVE_JETBRAINSANNOTATIONS 
namespace MetadataExtractor
{
    [System.AttributeUsage(System.AttributeTargets.All)] public class NotNullAttribute : Attribute {}
    [System.AttributeUsage(System.AttributeTargets.All)] public class ItemCanBeNull : Attribute {}
    [System.AttributeUsage(System.AttributeTargets.All)] public class CanBeNullAttribute : Attribute {}
    [System.AttributeUsage(System.AttributeTargets.All)] public class Pure : Attribute {}
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple=true)] public class SuppressMessageAttribute : Attribute { public SuppressMessageAttribute(string a, string b) {} }
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple=true)] public class ContractAnnotationAttribute : Attribute { public ContractAnnotationAttribute(string s) {} }
}
#endif

namespace MetadataExtractor
{
    using System.ComponentModel;
#if METADATAEXTRACTOR_HAVE_FILEMETADATA
    using MetadataExtractor.Formats.FileSystem;
#endif
    using MetadataExtractor.Formats.FileType;
    using MetadataExtractor.Formats.Jpeg;
    using MetadataExtractor.Formats.Png;
    using MetadataExtractor.Formats.QuickTime;
    using MetadataExtractor.Formats.Tiff;
#if METADATAEXTRACTOR_HAVE_UNDATED_SUPPORT
    using MetadataExtractor.Formats.Bmp;
    using MetadataExtractor.Formats.Gif;
    using MetadataExtractor.Formats.Ico;
    using MetadataExtractor.Formats.Netpbm;
    using MetadataExtractor.Formats.Pcx;
    using MetadataExtractor.Formats.Photoshop;
    using MetadataExtractor.Formats.Raf;
    using MetadataExtractor.Formats.WebP;
    using MetadataExtractor.Formats.Avi;
#endif

    /// <summary>Reads metadata from any supported file format.</summary>
    /// <remarks>
    /// This class a lightweight wrapper around other, specific metadata processors.
    /// During extraction, the file type is determined from the first few bytes of the file.
    /// Parsing is then delegated to one of:
    ///
    /// <list type="bullet">
    ///   <item><see cref="JpegMetadataReader"/> for JPEG files</item>
    ///   <item><see cref="TiffMetadataReader"/> for TIFF and (most) RAW files</item>
    ///   <item><see cref="PsdMetadataReader"/> for Photoshop files</item>
    ///   <item><see cref="PngMetadataReader"/> for PNG files</item>
    ///   <item><see cref="BmpMetadataReader"/> for BMP files</item>
    ///   <item><see cref="GifMetadataReader"/> for GIF files</item>
    ///   <item><see cref="IcoMetadataReader"/> for ICO files</item>
    ///   <item><see cref="NetpbmMetadataReader"/> for Netpbm files (PPM, PGM, PBM, PPM)</item>
    ///   <item><see cref="PcxMetadataReader"/> for PCX files</item>
    ///   <item><see cref="WebPMetadataReader"/> for WebP files</item>
    ///   <item><see cref="RafMetadataReader"/> for RAF files</item>
    ///   <item><see cref="QuickTimeMetadataReader"/> for QuickTime files</item>
    /// </list>
    ///
    /// If you know the file type you're working with, you may use one of the above processors directly.
    /// For most scenarios it is simpler, more convenient and more robust to use this class.
    /// <para />
    /// <see cref="FileTypeDetector"/> is used to determine the provided image's file type, and therefore
    /// the appropriate metadata reader to use.
    /// </remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public static class ImageMetadataReader
    {
        /// <summary>Reads metadata from an <see cref="Stream"/>.</summary>
        /// <param name="stream">A stream from which the file data may be read.  The stream must be positioned at the beginning of the file's data.</param>
        /// <returns>A list of <see cref="Directory"/> instances containing the various types of metadata found within the file's data.</returns>
        /// <exception cref="ImageProcessingException">The file type is unknown, or processing errors occurred.</exception>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] Stream stream)
        {
            var fileType = FileTypeDetector.DetectFileType(stream);

            var fileTypeDirectory = new FileTypeDirectory(fileType);
            
            switch (fileType)
            {
                case FileType.Jpeg:
                    return Append(JpegMetadataReader.ReadMetadata(stream), fileTypeDirectory);
                case FileType.Tiff:
                case FileType.Arw:
                case FileType.Cr2:
                case FileType.Nef:
                case FileType.Orf:
#if METADATAEXTRACTOR_HAVE_UNDATED_SUPPORT
                case FileType.Rw2:
#endif
                    return Append(TiffMetadataReader.ReadMetadata(stream), fileTypeDirectory);
#if METADATAEXTRACTOR_HAVE_UNDATED_SUPPORT
                case FileType.Psd:
                    return Append(PsdMetadataReader.ReadMetadata(stream), fileTypeDirectory);
#endif
                case FileType.Png:
                    return Append(PngMetadataReader.ReadMetadata(stream), fileTypeDirectory);
#if METADATAEXTRACTOR_HAVE_UNDATED_SUPPORT
                case FileType.Bmp:
                    return new Directory[] { BmpMetadataReader.ReadMetadata(stream), fileTypeDirectory };
                case FileType.Gif:
                    return Append(GifMetadataReader.ReadMetadata(stream), fileTypeDirectory);
                case FileType.Ico:
                    return Append(IcoMetadataReader.ReadMetadata(stream), fileTypeDirectory);
                case FileType.Pcx:
                    return new Directory[] { PcxMetadataReader.ReadMetadata(stream), fileTypeDirectory };
                case FileType.WebP:
                    return Append(WebPMetadataReader.ReadMetadata(stream), fileTypeDirectory);
                case FileType.Avi:
                    return Append(AviMetadataReader.ReadMetadata(stream), fileTypeDirectory);
                case FileType.Raf:
                    return Append(RafMetadataReader.ReadMetadata(stream), fileTypeDirectory);
#endif
                case FileType.QuickTime:
                    return Append(QuickTimeMetadataReader.ReadMetadata(stream), fileTypeDirectory);
#if METADATAEXTRACTOR_HAVE_UNDATED_SUPPORT
                case FileType.Netpbm:
                    return new Directory[] { NetpbmMetadataReader.ReadMetadata(stream), fileTypeDirectory };
#endif
                case FileType.Unknown:
                    throw new ImageProcessingException("File format could not be determined");
                case FileType.Riff:
                case FileType.Wav:
                case FileType.Crw:
                default:
                    throw new ImageProcessingException("File format is not supported");
            }
        }

        static DirectoryList Append(IEnumerable<Directory> list, Directory directory) 
                { return new List<Directory>(list) { directory }; }

        /// <summary>Reads metadata from a file.</summary>
        /// <remarks>Unlike <see cref="ReadMetadata(System.IO.Stream)"/>, this overload includes a <see cref="FileMetadataDirectory"/> in the output.</remarks>
        /// <param name="filePath">Location of a file from which data should be read.</param>
        /// <returns>A list of <see cref="Directory"/> instances containing the various types of metadata found within the file's data.</returns>
        /// <exception cref="ImageProcessingException">The file type is unknown, or processing errors occurred.</exception>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] string filePath)
        {
            var directories = new List<Directory>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                directories.AddRange(ReadMetadata(stream));

#if METADATAEXTRACTOR_HAVE_FILEMETADATA
            directories.Add(new FileMetadataReader().Read(filePath));
#endif

            return directories;
        }
    }

    /// <summary>Immutable type for representing a rational number.</summary>
    /// <remarks>
    /// Underlying values are stored as a numerator and denominator, each of type <see cref="long"/>.
    /// Note that any <see cref="Rational"/> with a numerator of zero will be treated as zero, even if the denominator is also zero.
    /// </remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
#if !NETSTANDARD1_3
    [Serializable]
    [TypeConverter(typeof(RationalConverter))]
#endif
    public struct Rational : IConvertible, IEquatable<Rational>
    {
        /// <summary>Gets the denominator.</summary>
        public long Denominator { get { return _Denominator; } }
        long _Denominator;

        /// <summary>Gets the numerator.</summary>
        public long Numerator { get { return _Numerator; } }
        long _Numerator;

        /// <summary>Initialises a new instance with the <paramref name="numerator"/> and <paramref name="denominator"/>.</summary>
        public Rational(long numerator, long denominator)
        {
            _Numerator = numerator;
            _Denominator = denominator;
        }

        #region Conversion methods

        /// <summary>Returns the value of the specified number as a <see cref="double"/>.</summary>
        /// <remarks>This may involve rounding.</remarks>
        public double ToDouble() { return Numerator == 0 ? 0.0 : Numerator/(double)Denominator; }

        /// <summary>Returns the value of the specified number as a <see cref="float"/>.</summary>
        /// <remarks>May incur rounding.</remarks>
        public float ToSingle() { return Numerator == 0 ? 0.0f : Numerator/(float)Denominator; }

        /// <summary>Returns the value of the specified number as a <see cref="byte"/>.</summary>
        /// <remarks>
        /// May incur rounding or truncation.  This implementation simply
        /// casts the result of <see cref="ToDouble"/> to <see cref="byte"/>.
        /// </remarks>
        public byte ToByte() { return (byte)ToDouble(); }

        /// <summary>Returns the value of the specified number as a <see cref="sbyte"/>.</summary>
        /// <remarks>
        /// May incur rounding or truncation.  This implementation simply
        /// casts the result of <see cref="ToDouble"/> to <see cref="sbyte"/>.
        /// </remarks>
        public sbyte ToSByte() { return (sbyte)ToDouble(); }

        /// <summary>Returns the value of the specified number as an <see cref="int"/>.</summary>
        /// <remarks>
        /// May incur rounding or truncation.  This implementation simply
        /// casts the result of <see cref="ToDouble"/> to <see cref="int"/>.
        /// </remarks>
        public int ToInt32() { return (int)ToDouble(); }

        /// <summary>Returns the value of the specified number as an <see cref="uint"/>.</summary>
        /// <remarks>
        /// May incur rounding or truncation.  This implementation simply
        /// casts the result of <see cref="ToDouble"/> to <see cref="uint"/>.
        /// </remarks>
        public uint ToUInt32() { return (uint)ToDouble(); }

        /// <summary>Returns the value of the specified number as a <see cref="long"/>.</summary>
        /// <remarks>
        /// May incur rounding or truncation.  This implementation simply
        /// casts the result of <see cref="ToDouble"/> to <see cref="long"/>.
        /// </remarks>
        public long ToInt64() { return (long)ToDouble(); }

        /// <summary>Returns the value of the specified number as a <see cref="ulong"/>.</summary>
        /// <remarks>
        /// May incur rounding or truncation.  This implementation simply
        /// casts the result of <see cref="ToDouble"/> to <see cref="ulong"/>.
        /// </remarks>
        public ulong ToUInt64() { return (ulong)ToDouble(); }

        /// <summary>Returns the value of the specified number as a <see cref="short"/>.</summary>
        /// <remarks>
        /// May incur rounding or truncation.  This implementation simply
        /// casts the result of <see cref="ToDouble"/> to <see cref="short"/>.
        /// </remarks>
        public short ToInt16() { return (short)ToDouble(); }

        /// <summary>Returns the value of the specified number as a <see cref="ushort"/>.</summary>
        /// <remarks>
        /// May incur rounding or truncation.  This implementation simply
        /// casts the result of <see cref="ToDouble"/> to <see cref="ushort"/>.
        /// </remarks>
        public ushort ToUInt16() { return (ushort)ToDouble(); }

        /// <summary>Returns the value of the specified number as a <see cref="decimal"/>.</summary>
        /// <remarks>May incur truncation.</remarks>
        public decimal ToDecimal() { return Denominator == 0 ? 0M : Numerator / (decimal)Denominator; }

        /// <summary>Returns <c>true</c> if the value is non-zero, otherwise <c>false</c>.</summary>
        public bool ToBoolean() { return Numerator != 0 && Denominator != 0; }

        #region IConvertible

        TypeCode IConvertible.GetTypeCode() { return TypeCode.Object; }

        bool IConvertible.ToBoolean(IFormatProvider provider) { return ToBoolean(); }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider) { return ToSByte(); }

        byte IConvertible.ToByte(IFormatProvider provider) { return ToByte(); }

        short IConvertible.ToInt16(IFormatProvider provider) { return ToInt16(); }

        ushort IConvertible.ToUInt16(IFormatProvider provider) { return ToUInt16(); }

        int IConvertible.ToInt32(IFormatProvider provider) { return ToInt32(); }

        uint IConvertible.ToUInt32(IFormatProvider provider) { return ToUInt32(); }

        long IConvertible.ToInt64(IFormatProvider provider) { return ToInt64(); }

        ulong IConvertible.ToUInt64(IFormatProvider provider) { return ToUInt64(); }

        float IConvertible.ToSingle(IFormatProvider provider) { return ToSingle(); }

        double IConvertible.ToDouble(IFormatProvider provider) { return ToDouble(); }

        decimal IConvertible.ToDecimal(IFormatProvider provider) { return ToDecimal(); }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        #endregion

        #endregion

        /// <summary>Gets the reciprocal value of this object as a new <see cref="Rational"/>.</summary>
        /// <value>the reciprocal in a new object</value>
        public Rational Reciprocal { get { return new Rational(Denominator, Numerator); } }

        /// <summary>
        /// Checks if this <see cref="Rational"/> number is expressible as an integer, either positive or negative.
        /// </summary>
        public bool IsInteger { get { return Denominator == 1 || (Denominator != 0 && Numerator%Denominator == 0) || (Denominator == 0 && Numerator == 0); } }

        /// <summary>
        /// True if either <see cref="Denominator"/> or <see cref="Numerator"/> are zero.
        /// </summary>
        public bool IsZero { get { return Denominator == 0 || Numerator == 0; } }

        #region Formatting

        /// <summary>Returns a string representation of the object of form <c>numerator/denominator</c>.</summary>
        /// <returns>a string representation of the object.</returns>
        public override string ToString() { return Numerator + "/" + Denominator; }

        public string ToString(IFormatProvider provider) { return Numerator.ToString(provider) + "/" + Denominator.ToString(provider); }

        /// <summary>
        /// Returns the simplest representation of this <see cref="Rational"/>'s value possible.
        /// </summary>
        [NotNull]
        public string ToSimpleString(bool allowDecimal = true, IFormatProvider provider = null)
        {
            if (Denominator == 0 && Numerator != 0)
                return ToString(provider);

            if (IsInteger)
                return ToInt32().ToString(provider);

            if (Numerator != 1 && Denominator%Numerator == 0)
            {
                // common factor between denominator and numerator
                var newDenominator = Denominator/Numerator;
                return new Rational(1, newDenominator).ToSimpleString(allowDecimal, provider);
            }

            var simplifiedInstance = GetSimplifiedInstance();
            if (allowDecimal)
            {
                var doubleString = simplifiedInstance.ToDouble().ToString(provider);
                if (doubleString.Length < 5)
                    return doubleString;
            }

            return simplifiedInstance.ToString(provider);
        }

        #endregion

        #region Equality and hashing

        /// <summary>
        /// Indicates whether this instance and <paramref name="other"/> are numerically equal,
        /// even if their representations differ.
        /// </summary>
        /// <remarks>
        /// For example, <c>1/2</c> is equal to <c>10/20</c> by this method.
        /// Similarly, <c>1/0</c> is equal to <c>100/0</c> by this method.
        /// To test equal representations, use <see cref="EqualsExact"/>.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Rational other) { return other.ToDecimal().Equals(ToDecimal()); }

        /// <summary>
        /// Indicates whether this instance and <paramref name="other"/> have identical
        /// <see cref="Numerator"/> and <see cref="Denominator"/>.
        /// </summary>
        /// <remarks>
        /// For example, <c>1/2</c> is not equal to <c>10/20</c> by this method.
        /// Similarly, <c>1/0</c> is not equal to <c>100/0</c> by this method.
        /// To test numerically equivalence, use <see cref="Equals(MetadataExtractor.Rational)"/>.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool EqualsExact(Rational other) { return Denominator == other.Denominator && Numerator == other.Numerator; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Rational && Equals((Rational)obj);
        }

        public override int GetHashCode() { return unchecked(Denominator.GetHashCode()*397) ^ Numerator.GetHashCode(); }

        #endregion

        /// <summary>
        /// Simplifies the representation of this <see cref="Rational"/> number.
        /// </summary>
        /// <remarks>
        /// For example, <c>5/10</c> simplifies to <c>1/2</c> because both <see cref="Numerator"/>
        /// and <see cref="Denominator"/> share a common factor of 5.
        /// <para />
        /// Uses the Euclidean Algorithm to find the greatest common divisor.
        /// </remarks>
        /// <returns>
        /// A simplified instance if one exists, otherwise a copy of the original value.
        /// </returns>
        public Rational GetSimplifiedInstance()
        {
            Func<long, long, long> GCD = (long a, long b) =>
            {
                if (a < 0)
                    a = -a;
                if (b < 0)
                    b = -b;

                while (a != 0 && b != 0)
                {
                    if (a > b)
                        a %= b;
                    else
                        b %= a;
                }

                return a == 0 ? b : a;
            };

            var gcd = GCD(Numerator, Denominator);

            return new Rational(Numerator / gcd, Denominator / gcd);
        }

        #region Equality operators

        public static bool operator==(Rational a, Rational b)
        {
            return Equals(a, b);
        }

        public static bool operator!=(Rational a, Rational b)
        {
            return !Equals(a, b);
        }

        #endregion

        #region RationalConverter

#if !NETSTANDARD1_3
        private sealed class RationalConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string) ||
                    sourceType == typeof(Rational) ||
                    typeof(IConvertible).IsAssignableFrom(sourceType) ||
                    (sourceType.IsArray && typeof(IConvertible).IsAssignableFrom(sourceType.GetElementType())))
                    return true;

                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                if (value == null)
                    return base.ConvertFrom(context, culture, null);

                var type = value.GetType();

                if (type == typeof(string))
                {
                    var v = ((string)value).Split('/');
                    long numerator, denominator;
                    if (v.Length == 2 && long.TryParse(v[0], out numerator) && long.TryParse(v[1], out denominator))
                        return new Rational(numerator, denominator);
                }

                if (type == typeof(Rational))
                    return value;

                if (type.IsArray)
                {
                    var array = (Array)value;
                    if (array.Rank == 1 && (array.Length == 1 || array.Length == 2))
                    {
                        return new Rational(
                            numerator: Convert.ToInt64(array.GetValue(0)),
                            denominator: array.Length == 2 ? Convert.ToInt64(array.GetValue(1)) : 1);
                    }
                }

                return new Rational(Convert.ToInt64(value), 1);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) { return false; }
        }
#endif

        #endregion
    }

    /// <summary>
    /// Wraps a byte array with an <see cref="Encoding"/>. Allows consumers to override the encoding if required.
    /// </summary>
    /// <remarks>
    /// String data is often in the incorrect format, and many issues have been raised in the past related to string
    /// encoding. Metadata Extractor used to decode string bytes at read-time, after which it was not possible to
    /// override the encoding at a later time by the user.
    /// <para />
    /// The introduction of this type allows full transparency and control over the use of string data extracted
    /// by the library during the read phase.
    /// </remarks>
    public struct StringValue : IConvertible
    {
        /// <summary>
        /// The encoding used when decoding a <see cref="StringValue"/> that does not specify its encoding.
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public StringValue([NotNull] byte[] bytes, Encoding encoding = null)
        {
            _Bytes = bytes;
            _Encoding = encoding;
        }

        [NotNull]
        public byte[] Bytes { get { return _Bytes; } }
        byte[] _Bytes;

        [CanBeNull]
        public Encoding Encoding { get { return _Encoding; } }
        Encoding _Encoding;

        #region IConvertible

        TypeCode IConvertible.GetTypeCode() { return TypeCode.Object; }

        string IConvertible.ToString(IFormatProvider provider) { return ToString(); }

        double IConvertible.ToDouble(IFormatProvider provider) { return double.Parse(ToString()); }

        decimal IConvertible.ToDecimal(IFormatProvider prodiver) { return decimal.Parse(ToString()); }

        float IConvertible.ToSingle(IFormatProvider provider) { return float.Parse(ToString()); }

        bool IConvertible.ToBoolean(IFormatProvider provider) { return bool.Parse(ToString()); }

        byte IConvertible.ToByte(IFormatProvider provider) { return byte.Parse(ToString()); }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            var s = ToString();
            if (s.Length != 1)
                throw new FormatException();
            return s[0];
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider) { return DateTime.Parse(ToString()); }

        short IConvertible.ToInt16(IFormatProvider provider) { return short.Parse(ToString()); }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            try
            {
                return int.Parse(ToString());
            }
            catch(Exception)
            {
                long val = 0;
                foreach(var b in Bytes)
                {
                    val = val << 8;
                    val += b & 0xff;
                }
                return (int)val;
            }
        }

        long IConvertible.ToInt64(IFormatProvider provider) { return long.Parse(ToString()); }

        sbyte IConvertible.ToSByte(IFormatProvider provider) { return sbyte.Parse(ToString()); }

        ushort IConvertible.ToUInt16(IFormatProvider provider) { return ushort.Parse(ToString()); }

        uint IConvertible.ToUInt32(IFormatProvider provider) { return uint.Parse(ToString()); }

        ulong IConvertible.ToUInt64(IFormatProvider provider) { return ulong.Parse(ToString()); }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider) { return Convert.ChangeType(ToString(), conversionType, provider); }

        #endregion

        #region Formatting

        public override string ToString() { return ToString(Encoding ?? DefaultEncoding); }

        [NotNull]
        public string ToString([NotNull] Encoding encoder) { return encoder.GetString(Bytes, 0, Bytes.Length); }

        #endregion
    }

    /// <summary>Base class for all metadata specific exceptions.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class MetadataException : Exception
    {
        public MetadataException([CanBeNull] string msg)
            : base(msg)
        {
        }

        public MetadataException([CanBeNull] Exception innerException)
            : base(null, innerException)
        {
        }

        public MetadataException([CanBeNull] string msg, [CanBeNull] Exception innerException)
            : base(msg, innerException)
        {
        }

#if !NETSTANDARD1_3
        protected MetadataException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>An exception class thrown upon an unexpected condition that was fatal for the processing of an image.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class ImageProcessingException : Exception
    {
        public ImageProcessingException([CanBeNull] string message)
            : base(message)
        {
        }

        public ImageProcessingException([CanBeNull] string message, [CanBeNull] Exception innerException)
            : base(message, innerException)
        {
        }

        public ImageProcessingException([CanBeNull] Exception innerException)
            : base(null, innerException)
        {
        }

#if !NETSTANDARD1_3
        protected ImageProcessingException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>Represents a latitude and longitude pair, giving a position on earth in spherical coordinates.</summary>
    /// <remarks>
    /// Values of latitude and longitude are given in degrees.
    /// <para />
    /// This type is immutable.
    /// </remarks>
    public sealed class GeoLocation
    {
        /// <summary>
        /// Initialises an instance of <see cref="GeoLocation"/>.
        /// </summary>
        /// <param name="latitude">the latitude, in degrees</param>
        /// <param name="longitude">the longitude, in degrees</param>
        public GeoLocation(double latitude, double longitude)
        {
            _Latitude = latitude;
            _Longitude = longitude;
        }

        /// <value>the latitudinal angle of this location, in degrees.</value>
        public double Latitude { get { return _Latitude; } }
        double _Latitude;

        /// <value>the longitudinal angle of this location, in degrees.</value>
        public double Longitude { get { return _Longitude; } }
        double _Longitude;

        /// <value>true, if both latitude and longitude are equal to zero</value>
        public bool IsZero { get { return Latitude == 0 && Longitude == 0; } }

        #region Static helpers/factories

        /// <summary>
        /// Converts a decimal degree angle into its corresponding DMS (degrees-minutes-seconds) representation as a string,
        /// of format:
        /// <c>-1° 23' 4.56"</c>
        /// </summary>
        [NotNull, Pure]
        public static string DecimalToDegreesMinutesSecondsString(double value)
        {
            var dms = DecimalToDegreesMinutesSeconds(value);
            return string.Format("{0:0.##}\u00b0 {1:0.##}' {2:0.##}\"", dms[0], dms[1], dms[2]);
        }

        /// <summary>
        /// Converts a decimal degree angle into its corresponding DMS (degrees-minutes-seconds) component values, as
        /// a double array.
        /// </summary>
        [NotNull, Pure]
        public static double[] DecimalToDegreesMinutesSeconds(double value)
        {
            var d = (int)value;
            var m = Math.Abs((value%1)*60);
            var s = (m%1)*60;
            return new[] { d, (int)m, s };
        }

        /// <summary>
        /// Converts DMS (degrees-minutes-seconds) rational values, as given in
        /// <see cref="GpsDirectory"/>, into a single value in degrees,
        /// as a double.
        /// </summary>
        [CanBeNull, Pure]
        public static double? DegreesMinutesSecondsToDecimal(Rational degs, Rational mins, Rational secs, bool isNegative)
        {
            var value = Math.Abs(degs.ToDouble()) + mins.ToDouble()/60.0d + secs.ToDouble()/3600.0d;
            if (double.IsNaN(value))
                return null;
            if (isNegative)
                value *= -1;
            return value;
        }

        #endregion

        #region Equality and Hashing

        private bool Equals([NotNull] GeoLocation other) { return Latitude.Equals(other.Latitude) &&
                                                  Longitude.Equals(other.Longitude); }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is GeoLocation && Equals((GeoLocation)obj);
        }

        public override int GetHashCode() { return unchecked((Latitude.GetHashCode()*397) ^ Longitude.GetHashCode()); }

        #endregion

        #region ToString

        /// <returns>
        /// Returns a string representation of this object, of format:
        /// <c>1.23, 4.56</c>
        /// </returns>
        public override string ToString() { return Latitude + ", " + Longitude; }

        /// <returns>
        /// a string representation of this location, of format:
        /// <c>-1° 23' 4.56", 54° 32' 1.92"</c>
        /// </returns>
        [NotNull, Pure]
        public string ToDmsString() { return DecimalToDegreesMinutesSecondsString(Latitude) + ", " + DecimalToDegreesMinutesSecondsString(Longitude); }

        #endregion
    }

    public interface ITagDescriptor
    {
        /// <summary>Decodes the raw value stored for <paramref name="tagType"/>.</summary>
        /// <remarks>
        /// Where possible, known values will be substituted here in place of the raw
        /// tokens actually kept in the metadata segment.  If no substitution is
        /// available, the value provided by <see cref="DirectoryExtensions.GetString(MetadataExtractor.Directory,int)"/> will be returned.
        /// </remarks>
        /// <param name="tagType">The tag to find a description for.</param>
        /// <returns>
        /// A description of the image's value for the specified tag, or
        /// <c>null</c> if the tag hasn't been defined.
        /// </returns>
        [CanBeNull]
        string GetDescription(int tagType);
    }

    /// <summary>Base class for all tag descriptor classes.</summary>
    /// <remarks>
    /// Implementations are responsible for providing the human-readable string representation of tag values stored in a directory.
    /// The directory is provided to the tag descriptor via its constructor.
    /// </remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public class TagDescriptor<T> : ITagDescriptor
        where T : Directory
    {
        [NotNull]
        protected readonly T Directory;

        public TagDescriptor([NotNull] T directory)
        {
            Directory = directory;
        }

        /// <summary>Returns a descriptive value of the specified tag for this image.</summary>
        /// <remarks>
        /// Where possible, known values will be substituted here in place of the raw
        /// tokens actually kept in the metadata segment.  If no substitution is
        /// available, the value provided by <c>getString(tagType)</c> will be returned.
        /// </remarks>
        /// <param name="tagType">the tag to find a description for</param>
        /// <returns>
        /// a description of the image's value for the specified tag, or
        /// <c>null</c> if the tag hasn't been defined.
        /// </returns>
        public virtual string GetDescription(int tagType)
        {
            var obj = Directory.GetObject(tagType);
            if (obj == null)
                return null;

            // special presentation for long arrays
            if (obj is Array && ((Array)obj).Length > 16)
                return "[" + ((Array)obj).Length + " " + (((Array)obj).Length == 1 ? "value" : "values") + "]";

            // no special handling required, so use default conversion to a string
            return Directory.GetString(tagType);
        }

        /// <summary>
        /// Takes a series of 4 bytes from the specified offset, and converts these to a
        /// well-known version number, where possible.
        /// </summary>
        /// <remarks>
        /// Two different formats are processed:
        /// <list type="bullet">
        /// <item>[30 32 31 30] -&gt; 2.10</item>
        /// <item>[0 1 0 0] -&gt; 1.00</item>
        /// </list>
        /// </remarks>
        /// <param name="components">the four version values</param>
        /// <param name="majorDigits">the number of components to be</param>
        /// <returns>the version as a string of form "2.10" or null if the argument cannot be converted</returns>
        [Pure]
        [CanBeNull]
        public static string ConvertBytesToVersionString([CanBeNull] int[] components, int majorDigits)
        {
            if (components == null)
                return null;

            var version = new StringBuilder();
            for (var i = 0; i < 4 && i < components.Length; i++)
            {
                if (i == majorDigits)
                    version.Append('.');
                var c = (char)components[i];
                if (c < '0')
                    c += '0';
                if (i == 0 && c == '0')
                    continue;
                version.Append(c);
            }
            return version.ToString();
        }

        [Pure]
        [CanBeNull]
        protected string GetVersionBytesDescription(int tagType, int majorDigits)
        {
            var values = Directory.GetInt32Array(tagType);
            return values == null ? null : ConvertBytesToVersionString(values, majorDigits);
        }

        [Pure]
        [CanBeNull]
        protected string GetIndexedDescription(int tagType, [NotNull] params string[] descriptions)
        {
            return GetIndexedDescription(tagType, 0, descriptions);
        }

        [Pure]
        [CanBeNull]
        protected string GetIndexedDescription(int tagType, int baseIndex, [NotNull] params string[] descriptions)
        {
            uint index;
            if (!Directory.TryGetUInt32(tagType, out index))
                return null;

            var arrayIndex = index - baseIndex;

            if (arrayIndex >= 0 && arrayIndex < descriptions.Length)
            {
                var description = descriptions[arrayIndex];
                if (description != null)
                    return description;
            }

            return "Unknown (" + index + ")";
        }

        [Pure]
        [CanBeNull]
        protected string GetByteLengthDescription(int tagType)
        {
            var bytes = Directory.GetByteArray(tagType);
            if (bytes == null)
                return null;
            return "(" + bytes.Length + " byte" + (bytes.Length == 1 ? string.Empty : "s") + ")";
        }

        [Pure]
        [CanBeNull]
        protected string GetSimpleRational(int tagType)
        {
            Rational value;
            if (!Directory.TryGetRational(tagType, out value))
                return null;
            return value.ToSimpleString();
        }

        [Pure]
        [CanBeNull]
        protected string GetDecimalRational(int tagType, int decimalPlaces)
        {
            Rational value;
            if (!Directory.TryGetRational(tagType, out value))
                return null;
            return string.Format("{0:F" + decimalPlaces + "}", value.ToDouble());
        }

        [Pure]
        [CanBeNull]
        protected string GetFormattedInt(int tagType, [NotNull] string format)
        {
            int value;
            if (!Directory.TryGetInt32(tagType, out value))
                return null;
            return string.Format(format, value);
        }

        [Pure]
        [CanBeNull]
        protected string GetFormattedString(int tagType, [NotNull] string format)
        {
            var value = Directory.GetString(tagType);
            if (value == null)
                return null;
            return string.Format(format, value);
        }

        [Pure]
        [CanBeNull]
        protected string GetEpochTimeDescription(int tagType)
        {
            // TODO have observed a byte[8] here which is likely some kind of date (ticks as long?)
            long value;
            return Directory.TryGetInt64(tagType, out value)
                ? DateUtil.FromUnixTime(value).ToString("ddd MMM dd HH:mm:ss zzz yyyy")
                : null;
        }

        /// <remarks>LSB first. Labels may be null, a String, or a String[2] with (low label,high label) values.</remarks>
        [Pure]
        [CanBeNull]
        protected string GetBitFlagDescription(int tagType, [NotNull] params object[] labels)
        {
            int value;
            if (!Directory.TryGetInt32(tagType, out value))
                return null;
            var parts = new List<string>();
            var bitIndex = 0;
            while (labels.Length > bitIndex)
            {
                var labelObj = labels[bitIndex];
                if (labelObj != null)
                {
                    var isBitSet = (value & 1) == 1;
                    if (labelObj is string[])
                    {
                        var labelPair = (string[])labelObj;
                        Debug.Assert(labelPair.Length == 2);
                        parts.Add(labelPair[isBitSet ? 1 : 0]);
                    }
                    else if (isBitSet && labelObj is string)
                    {
                        parts.Add((string)labelObj);
                    }
                }
                value >>= 1;
                bitIndex++;
            }
#if DOTNET35
            return string.Join(", ", parts.ToArray());
#else
            return string.Join(", ", parts);
#endif
        }

        [Pure]
        [CanBeNull]
        protected string GetStringFrom7BitBytes(int tagType)
        {
            var bytes = Directory.GetByteArray(tagType);
            if (bytes == null)
                return null;
            var length = bytes.Length;
            for (var index = 0; index < bytes.Length; index++)
            {
                var i = bytes[index] & 0xFF;
                if (i == 0 || i > 0x7F)
                {
                    length = index;
                    break;
                }
            }
            return Encoding.UTF8.GetString(bytes, 0, length);
        }

        [Pure]
        [CanBeNull]
        protected string GetStringFromUtf8Bytes(int tag)
        {
            var values = Directory.GetByteArray(tag);
            if (values == null)
                return null;

            try
            {
                return Encoding.UTF8
                    .GetString(values, 0, values.Length)
                    .Trim('\0', ' ', '\r', '\n', '\t');
            }
            catch
            {
                return null;
            }
        }

        [Pure]
        [CanBeNull]
        protected string GetRationalOrDoubleString(int tagType)
        {
            Rational rational;
            if (Directory.TryGetRational(tagType, out rational))
                return rational.ToSimpleString();

            double d;
            if (Directory.TryGetDouble(tagType, out d))
                return d.ToString("0.###");

            return null;
        }

        [Pure]
        [NotNull]
        protected static string GetFStopDescription(double fStop) { return string.Format("f/{0:0.0}", Math.Round(fStop, 1, MidpointRounding.AwayFromZero)); }

        [Pure]
        [NotNull]
        protected static string GetFocalLengthDescription(double mm) { return string.Format("{0:0.#} mm", mm); }

        [Pure]
        [CanBeNull]
        protected string GetLensSpecificationDescription(int tagId)
        {
            var values = Directory.GetRationalArray(tagId);

            if (values == null || values.Length != 4 || values[0].IsZero && values[2].IsZero)
                return null;

            var sb = new StringBuilder();

            if (values[0] == values[1])
                sb.Append(values[0].ToSimpleString()).Append("mm");
            else
                sb.Append(values[0].ToSimpleString()).Append("-").Append(values[1].ToSimpleString()).Append("mm");

            if (!values[2].IsZero)
            {
                sb.Append(' ');

                if (values[2] == values[3])
                    sb.Append(GetFStopDescription(values[2].ToDouble()));
                else
                    sb.Append("f/")
#if !NETSTANDARD1_3
                      .Append(Math.Round(values[2].ToDouble(), 1, MidpointRounding.AwayFromZero).ToString("0.0"))
#else
                      .Append(Math.Round(values[2].ToDouble(), 1).ToString("0.0"))
#endif
                      .Append("-")
#if !NETSTANDARD1_3
                      .Append(Math.Round(values[3].ToDouble(), 1, MidpointRounding.AwayFromZero).ToString("0.0"));
#else
                      .Append(Math.Round(values[3].ToDouble(), 1).ToString("0.0"));
#endif
            }

            return sb.ToString();
        }

        [CanBeNull]
        protected string GetOrientationDescription(int tag)
        {
            return GetIndexedDescription(tag, 1,
                "Top, left side (Horizontal / normal)",
                "Top, right side (Mirror horizontal)",
                "Bottom, right side (Rotate 180)", "Bottom, left side (Mirror vertical)",
                "Left side, top (Mirror horizontal and rotate 270 CW)",
                "Right side, top (Rotate 90 CW)",
                "Right side, bottom (Mirror horizontal and rotate 90 CW)",
                "Left side, bottom (Rotate 270 CW)");
        }

        [CanBeNull]
        protected string GetShutterSpeedDescription(int tagId)
        {
            // I believe this method to now be stable, but am leaving some alternative snippets of
            // code in here, to assist anyone who's looking into this (given that I don't have a public CVS).
            //        float apexValue = _directory.getFloat(ExifSubIFDDirectory.TAG_SHUTTER_SPEED);
            //        int apexPower = (int)Math.pow(2.0, apexValue);
            //        return "1/" + apexPower + " sec";
            // TODO test this method
            // thanks to Mark Edwards for spotting and patching a bug in the calculation of this
            // description (spotted bug using a Canon EOS 300D)
            // thanks also to Gli Blr for spotting this bug
            float apexValue;
            if (!Directory.TryGetSingle(tagId, out apexValue))
                return null;

            if (apexValue <= 1)
            {
                var apexPower = (float)(1 / Math.Exp(apexValue * Math.Log(2)));
                var apexPower10 = (long)Math.Round(apexPower * 10.0);
                var fApexPower = apexPower10 / 10.0f;
                return fApexPower + " sec";
            }
            else
            {
                var apexPower = (int)Math.Exp(apexValue * Math.Log(2));
                return "1/" + apexPower + " sec";
            }
        }

        // EXIF LightSource
        [CanBeNull]
        protected string GetLightSourceDescription(ushort wbtype)
        {
            switch (wbtype)
            {
                case 0:
                    return "Unknown";
                case 1:
                    return "Daylight";
                case 2:
                    return "Fluorescent";
                case 3:
                    return "Tungsten (Incandescent)";
                case 4:
                    return "Flash";
                case 9:
                    return "Fine Weather";
                case 10:
                    return "Cloudy";
                case 11:
                    return "Shade";
                case 12:
                    return "Daylight Fluorescent";    // (D 5700 - 7100K)
                case 13:
                    return "Day White Fluorescent";   // (N 4600 - 5500K)
                case 14:
                    return "Cool White Fluorescent";  // (W 3800 - 4500K)
                case 15:
                    return "White Fluorescent";       // (WW 3250 - 3800K)
                case 16:
                    return "Warm White Fluorescent";  // (L 2600 - 3250K)
                case 17:
                    return "Standard Light A";
                case 18:
                    return "Standard Light B";
                case 19:
                    return "Standard Light C";
                case 20:
                    return "D55";
                case 21:
                    return "D65";
                case 22:
                    return "D75";
                case 23:
                    return "D50";
                case 24:
                    return "ISO Studio Tungsten";
                case 255:
                    return "Other";
            }

            return GetDescription(wbtype);
        }
    }

    /// <summary>
    /// Models metadata of a tag within a <see cref="Directory"/> and provides methods
    /// for obtaining its value.
    /// </summary>
    /// <remarks>Immutable.</remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class Tag
    {
        [NotNull]
        private readonly Directory _directory;

        public Tag(int type, [NotNull] Directory directory)
        {
            Type = type;
            _directory = directory;
        }

        /// <summary>Gets the tag type as an int</summary>
        /// <value>the tag type as an int</value>
        public int Type { get; private set; }

        [Obsolete("Use Type instead.")]
        public int TagType { get { return Type; } }

        /// <summary>
        /// Get a description of the tag's value, considering enumerated values
        /// and units.
        /// </summary>
        /// <value>a description of the tag's value</value>
        [CanBeNull]
        public string Description { get { return _directory.GetDescription(Type); } }

        /// <summary>Get whether this tag has a name.</summary>
        /// <remarks>
        /// If <c>true</c>, it may be accessed via <see cref="Name"/>.
        /// If <c>false</c>, <see cref="Name"/> will return a string resembling <c>"Unknown tag (0x1234)"</c>.
        /// </remarks>
        public bool HasName { get { return _directory.HasTagName(Type); } }

        [Obsolete("Use HasName instead.")]
        public bool HasTagName { get { return HasName; } }

        /// <summary>
        /// Get the name of the tag, such as <c>Aperture</c>, or <c>InteropVersion</c>.
        /// </summary>
        [NotNull]
        public string Name { get { return _directory.GetTagName(Type); } }

        [NotNull]
        [Obsolete("Use Name instead")]
        public string TagName { get { return Name; } }

        /// <summary>
        /// Get the name of the <see cref="Directory"/> in which the tag exists, such as <c>Exif</c>, <c>GPS</c> or <c>Interoperability</c>.
        /// </summary>
        [NotNull]
        public string DirectoryName { get { return _directory.Name; } }

        /// <summary>A basic representation of the tag's type and value.</summary>
        /// <remarks>EG: <c>[ExifIfd0] F Number - f/2.8</c>.</remarks>
        /// <returns>The tag's type and value.</returns>
        public override string ToString() { return string.Format("[{0}] {1} - {2}", DirectoryName, Name, Description ?? _directory.GetString(Type) + " (unable to formulate description)"); }
    }

    /// <summary>
    /// Abstract base class for all directory implementations, having methods for getting and setting tag values of various
    /// data types.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public abstract class Directory
    {
#if NETSTANDARD1_3
        static Directory()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
#endif

        /// <summary>Map of values hashed by type identifiers.</summary>
        [NotNull]
        private readonly Dictionary<int, object> _tagMap = new Dictionary<int, object>();

        /// <summary>Holds tags in the order in which they were stored.</summary>
        [NotNull]
        private readonly List<Tag> _definedTagList = new List<Tag>();

        [NotNull]
        private readonly List<string> _errorList = new List<string>(capacity: 4);

        /// <summary>The descriptor used to interpret tag values.</summary>
        private ITagDescriptor _descriptor;

        /// <summary>Provides the name of the directory, for display purposes.</summary>
        /// <value>the name of the directory</value>
        [NotNull]
        public abstract string Name { get; }

        /// <summary>
        /// The parent <see cref="Directory"/>, when available, which may be used to construct information about the hierarchical structure of metadata.
        /// </summary>
        [CanBeNull]
        public Directory Parent { get; internal set; }

        /// <summary>Attempts to find the name of the specified tag.</summary>
        /// <param name="tagType">The tag to look up.</param>
        /// <param name="tagName">The found name, if any.</param>
        /// <returns><c>true</c> if the tag is known and <paramref name="tagName"/> was set, otherwise <c>false</c>.</returns>
        [ContractAnnotation("=>false,tagName:null")]
        [ContractAnnotation("=>true, tagName:notnull")]
        protected abstract bool TryGetTagName(int tagType, out string tagName);

        /// <summary>Gets a value indicating whether the directory is empty, meaning it contains no errors and no tag values.</summary>
        public bool IsEmpty { get { return _errorList.Count == 0 && _definedTagList.Count == 0; } }

        /// <summary>Indicates whether the specified tag type has been set.</summary>
        /// <param name="tagType">the tag type to check for</param>
        /// <returns>true if a value exists for the specified tag type, false if not</returns>
        public bool ContainsTag(int tagType) { return _tagMap.ContainsKey(tagType); }

        /// <summary>Returns all <see cref="Tag"/> objects that have been set in this <see cref="Directory"/>.</summary>
        /// <value>The list of <see cref="Tag"/> objects.</value>
        [NotNull]
        public
#if DOTNET35
            IEnumerable<Tag>
#else
            IReadOnlyList<Tag>
#endif
            Tags { get { return _definedTagList; } }

        /// <summary>Returns the number of tags set in this Directory.</summary>
        /// <value>the number of tags set in this Directory</value>
        public int TagCount { get { return _definedTagList.Count; } }

        /// <summary>Sets the descriptor used to interpret tag values.</summary>
        /// <param name="descriptor">the descriptor used to interpret tag values</param>
        protected void SetDescriptor([NotNull] ITagDescriptor descriptor)
        {
            if ((_descriptor = descriptor) == null) throw new ArgumentNullException("descriptor");
        }

        #region Errors

        /// <summary>Registers an error message with this directory.</summary>
        /// <param name="message">an error message.</param>
        public void AddError([NotNull] string message) { _errorList.Add(message); }

        /// <summary>Gets a value indicating whether this directory has one or more errors.</summary>
        /// <remarks>Error messages are accessible via <see cref="Errors"/>.</remarks>
        /// <returns><c>true</c> if the directory contains errors, otherwise <c>false</c></returns>
        public bool HasError { get { return _errorList.Count > 0; } }

        /// <summary>Used to iterate over any error messages contained in this directory.</summary>
        /// <value>The collection of error message strings.</value>
        [NotNull]
        public
#if DOTNET35
            IEnumerable<string>
#else
            IReadOnlyList<string>
#endif
            Errors { get { return _errorList; } }

        #endregion

        #region Get / set values

        /// <summary>Sets a <c>Object</c> for the specified tag.</summary>
        /// <remarks>Any previous value for this tag is overwritten.</remarks>
        /// <param name="tagType">the tag's value as an int</param>
        /// <param name="value">the value for the specified tag</param>
        /// <exception cref="ArgumentNullException">if value is <c>null</c></exception>
        public virtual void Set(int tagType, [NotNull] object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (!_tagMap.ContainsKey(tagType))
                _definedTagList.Add(new Tag(tagType, this));

            _tagMap[tagType] = value;
        }

        /// <summary>Returns the object hashed for the particular tag type specified, if available.</summary>
        /// <param name="tagType">the tag type identifier</param>
        /// <returns>the tag's value as an Object if available, else <c>null</c></returns>
        [CanBeNull]
        public object GetObject(int tagType)
        {
            object val;
            return _tagMap.TryGetValue(tagType, out val) ? val : null;
        }

        #endregion

        /// <summary>Returns the name of a specified tag as a String.</summary>
        /// <param name="tagType">the tag type identifier</param>
        /// <returns>the tag's name as a String</returns>
        [NotNull]
        public string GetTagName(int tagType)
        {
            string name;
            return !TryGetTagName(tagType, out name)
                ? string.Format("Unknown tag (0x{0:x4})", tagType)
                : name;
        }

        /// <summary>Gets whether the specified tag is known by the directory and has a name.</summary>
        /// <param name="tagType">the tag type identifier</param>
        /// <returns>whether this directory has a name for the specified tag</returns>
        public bool HasTagName(int tagType) { string _; return TryGetTagName(tagType, out _); }

        /// <summary>
        /// Provides a description of a tag's value using the descriptor set by <see cref="SetDescriptor"/>.
        /// </summary>
        /// <param name="tagType">the tag type identifier</param>
        /// <returns>the tag value's description as a String</returns>
        [CanBeNull]
        public string GetDescription(int tagType)
        {
            Debug.Assert(_descriptor != null);
            return _descriptor.GetDescription(tagType);
        }

        public override string ToString() { return string.Format("{0} Directory ({1} {2})", Name, _tagMap.Count, (_tagMap.Count == 1 ? "tag" : "tags")); }
    }

    /// <summary>
    /// A directory to use for the reporting of errors. No values may be added to this directory, only warnings and errors.
    /// </summary>
    public sealed class ErrorDirectory : Directory
    {
        public override string Name { get { return "Error"; } }

        public ErrorDirectory() { }

        public ErrorDirectory(string error) { AddError(error); }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            tagName = null;
            return false;
        }

        public override void Set(int tagType, object value) { throw new NotSupportedException("Cannot add values to " + typeof(ErrorDirectory).Name + "."); }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class DirectoryExtensions
    {
        #region Byte

        /// <summary>Returns a tag's value as a <see cref="byte"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        [Pure]
        public static byte GetByte([NotNull] this Directory directory, int tagType)
        {
            byte value;
            if (directory.TryGetByte(tagType, out value))
                return value;

            return ThrowValueNotPossible<byte>(directory, tagType);
        }

        [Pure]
        public static bool TryGetByte([NotNull] this Directory directory, int tagType, out byte value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToByte(null);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            value = default(byte);
            return false;
        }

        #endregion

        #region Int16

        /// <summary>Returns a tag's value as a <see cref="short"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        [Pure]
        public static short GetInt16([NotNull] this Directory directory, int tagType)
        {
            short value;
            if (directory.TryGetInt16(tagType, out value))
                return value;

            return ThrowValueNotPossible<short>(directory, tagType);
        }

        [Pure]
        public static bool TryGetInt16([NotNull] this Directory directory, int tagType, out short value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToInt16(null);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            value = default(short);
            return false;
        }

        #endregion

        #region UInt16

        /// <summary>Returns a tag's value as a <see cref="ushort"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        [Pure]
        public static ushort GetUInt16([NotNull] this Directory directory, int tagType)
        {
            ushort value;
            if (directory.TryGetUInt16(tagType, out value))
                return value;

            return ThrowValueNotPossible<ushort>(directory, tagType);
        }

        [Pure]
        public static bool TryGetUInt16([NotNull] this Directory directory, int tagType, out ushort value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToUInt16(null);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            value = default(ushort);
            return false;
        }

        #endregion

        #region Int32

        /// <summary>Returns a tag's value as an <see cref="int"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        [Pure]
        public static int GetInt32([NotNull] this Directory directory, int tagType)
        {
            int value;
            if (directory.TryGetInt32(tagType, out value))
                return value;

            return ThrowValueNotPossible<int>(directory, tagType);
        }

        [Pure]
        public static bool TryGetInt32([NotNull] this Directory directory, int tagType, out int value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToInt32(null);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            value = default(int);
            return false;
        }

        #endregion

        #region UInt32

        /// <summary>Returns a tag's value as a <see cref="uint"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        public static uint GetUInt32(this Directory directory, int tagType)
        {
            uint value;
            if (directory.TryGetUInt32(tagType, out value))
                return value;

            return ThrowValueNotPossible<ushort>(directory, tagType);
        }

        public static bool TryGetUInt32(this Directory directory, int tagType, out uint value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToUInt32(null);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            value = default(uint);
            return false;
        }

        #endregion

        #region Int64

        /// <summary>Returns a tag's value as an <see cref="int"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        [Pure]
        public static long GetInt64([NotNull] this Directory directory, int tagType)
        {
            int value;
            if (directory.TryGetInt32(tagType, out value))
                return value;

            return ThrowValueNotPossible<long>(directory, tagType);
        }

        [Pure]
        public static bool TryGetInt64([NotNull] this Directory directory, int tagType, out long value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToInt64(null);
                    return true;
                }
                catch
                {
                    // ignored
                    // ignored
                }
            }

            value = default(long);
            return false;
        }

        #endregion

        #region Single

        /// <summary>Returns a tag's value as a <see cref="float"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        [Pure]
        public static float GetSingle([NotNull] this Directory directory, int tagType)
        {
            float value;
            if (directory.TryGetSingle(tagType, out value))
                return value;

            return ThrowValueNotPossible<float>(directory, tagType);
        }

        [Pure]
        public static bool TryGetSingle([NotNull] this Directory directory, int tagType, out float value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToSingle(null);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            value = default(float);
            return false;
        }

        #endregion

        #region Double

        /// <summary>Returns a tag's value as an <see cref="double"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        [Pure]
        public static double GetDouble([NotNull] this Directory directory, int tagType)
        {
            double value;
            if (directory.TryGetDouble(tagType, out value))
                return value;

            return ThrowValueNotPossible<double>(directory, tagType);
        }

        [Pure]
        public static bool TryGetDouble([NotNull] this Directory directory, int tagType, out double value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToSingle(null);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            value = default(double);
            return false;
        }

        #endregion

        #region Boolean

        /// <summary>Returns a tag's value as an <see cref="bool"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        [Pure]
        public static bool GetBoolean([NotNull] this Directory directory, int tagType)
        {
            bool value;
            if (directory.TryGetBoolean(tagType, out value))
                return value;

            return ThrowValueNotPossible<bool>(directory, tagType);
        }

        [Pure]
        public static bool TryGetBoolean([NotNull] this Directory directory, int tagType, out bool value)
        {
            var convertible = GetConvertibleObject(directory, tagType);

            if (convertible != null)
            {
                try
                {
                    value = convertible.ToBoolean(null);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            value = default(bool);
            return false;
        }

        #endregion

        /// <summary>Gets the specified tag's value as a String array, if possible.</summary>
        /// <remarks>Only supported where the tag is set as String[], String, int[], byte[] or Rational[].</remarks>
        /// <returns>the tag's value as an array of Strings. If the value is unset or cannot be converted, <c>null</c> is returned.</returns>
        [Pure]
        [CanBeNull]
        public static string[] GetStringArray([NotNull] this Directory directory, int tagType)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
                return null;

            if (o is string[])
                return (string[])o;

            if (o is string)
                return new[] { (string)o };

            if (o is StringValue)
                return new[] { o.ToString() };

            if (o is StringValue[])
            {
                StringValue[] stringValues = (StringValue[])o;
                var strs = new string[stringValues.Length];
                for (var i = 0; i < strs.Length; i++)
                    strs[i] = stringValues[i].ToString();
                return strs;
            }

            if (o is int[])
            {
                int[] ints = (int[])o;
                string[] strings = new string[ints.Length];
                for (var i = 0; i < strings.Length; i++)
                    strings[i] = ints[i].ToString();
                return strings;
            }

            if (o is byte[])
            {
                byte[] bytes = (byte[])o;
                string[] strings = new string[bytes.Length];
                for (var i = 0; i < strings.Length; i++)
                    strings[i] = ((int)bytes[i]).ToString();
                return strings;
            }

            if (o is Rational[])
            {
                Rational[] rationals = (Rational[])o;
                string[] strings = new string[rationals.Length];
                for (var i = 0; i < strings.Length; i++)
                    strings[i] = rationals[i].ToSimpleString(false);
                return strings;
            }

            return null;
        }

        /// <summary>Gets the specified tag's value as a StringValue array, if possible.</summary>
        /// <remarks>Only succeeds if the tag is set as StringValue[], or String.</remarks>
        /// <returns>the tag's value as an array of StringValues. If the value is unset or cannot be converted, <c>null</c> is returned.</returns>
        [Pure]
        [CanBeNull]
        public static StringValue[] GetStringValueArray([NotNull] this Directory directory, int tagType)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
                return null;
            if (o is StringValue[])
                return (StringValue[])o;
            if (o is StringValue)
                return new [] { (StringValue)o };

            return null;
        }

        /// <summary>Gets the specified tag's value as an int array, if possible.</summary>
        /// <remarks>Only supported where the tag is set as String, Integer, int[], byte[] or Rational[].</remarks>
        /// <returns>the tag's value as an int array</returns>
        [Pure]
        [CanBeNull]
        public static int[] GetInt32Array([NotNull] this Directory directory, int tagType)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
                return null;

            if (o is int[])
                return (int[])o;

            if (o is Rational[])
            {
                Rational[] rationals = (Rational[])o;
                int[] ints = new int[rationals.Length];
                for (var i = 0; i < ints.Length; i++)
                    ints[i] = rationals[i].ToInt32();
                return ints;
            }

            if (o is short[])
            {
                short[] shorts = (short[])o;
                int[] ints = new int[shorts.Length];
                for (var i = 0; i < shorts.Length; i++)
                    ints[i] = shorts[i];
                return ints;
            }

            if (o is sbyte[])
            {
                sbyte[] sbytes = (sbyte[])o;
                int[] ints = new int[sbytes.Length];
                for (var i = 0; i < sbytes.Length; i++)
                    ints[i] = sbytes[i];
                return ints;
            }

            if (o is byte[])
            {
                byte[] bytes = (byte[])o;
                int[] ints = new int[bytes.Length];
                for (var i = 0; i < bytes.Length; i++)
                    ints[i] = bytes[i];
                return ints;
            }

            if (o is string)
            {
                string str = (string)o;
                int[] ints = new int[str.Length];
                for (var i = 0; i < str.Length; i++)
                    ints[i] = str[i];
                return ints;
            }

            var nullableInt = o as int?;
            if (nullableInt != null)
                return new[] { (int)o };

            return null;
        }

        /// <summary>Gets the specified tag's value as an byte array, if possible.</summary>
        /// <remarks>Only supported where the tag is set as StringValue, String, Integer, int[], byte[] or Rational[].</remarks>
        /// <returns>the tag's value as a byte array</returns>
        [Pure]
        [CanBeNull]
        public static byte[] GetByteArray([NotNull] this Directory directory, int tagType)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
                return null;

            if (o is StringValue)
                return ((StringValue)o).Bytes;

            byte[] bytes;

            if (o is Rational[])
            {
                Rational[] rationals = (Rational[])o;
                bytes = new byte[rationals.Length];
                for (var i = 0; i < bytes.Length; i++)
                    bytes[i] = rationals[i].ToByte();
                return bytes;
            }

            bytes = o as byte[];
            if (bytes != null)
                return bytes;

            if (o is int[])
            {
                int[] ints = (int[])o;
                bytes = new byte[ints.Length];
                for (var i = 0; i < ints.Length; i++)
                    bytes[i] = unchecked((byte)ints[i]);
                return bytes;
            }

            if (o is short[])
            {
                short[] shorts = (short[])o;
                bytes = new byte[shorts.Length];
                for (var i = 0; i < shorts.Length; i++)
                    bytes[i] = unchecked((byte)shorts[i]);
                return bytes;
            }

            if (o is string)
            {
                string str = (string)o;
                bytes = new byte[str.Length];
                for (var i = 0; i < str.Length; i++)
                    bytes[i] = unchecked((byte)str[i]);
                return bytes;
            }

            var nullableInt = o as int?;
            if (nullableInt != null)
                return new[] { (byte)nullableInt.Value };

            return null;
        }

        #region DateTime

        /// <summary>Returns a tag's value as a <see cref="DateTime"/>, or throws if conversion is not possible.</summary>
        /// <remarks>
        /// If the value is <see cref="IConvertible"/>, then that interface is used for conversion of the value.
        /// If the value is an array of <see cref="IConvertible"/> having length one, then the single item is converted.
        /// </remarks>
        /// <exception cref="MetadataException">No value exists for <paramref name="tagType"/>, or the value is not convertible to the requested type.</exception>
        public static DateTime GetDateTime([NotNull] this Directory directory, int tagType /*, [CanBeNull] TimeZoneInfo timeZone = null*/)
        {
            DateTime value;
            if (directory.TryGetDateTime(tagType, out value))
                return value;

            return ThrowValueNotPossible<DateTime>(directory, tagType);
        }

        // This seems to cover all known Exif date strings
        // Note that "    :  :     :  :  " is a valid date string according to the Exif spec (which means 'unknown date'): http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/datetimeoriginal.html
        // Custom format reference: https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx
        private static readonly string[] _datePatterns =
        {
            "yyyy:MM:dd HH:mm:ss.fff",
            "yyyy:MM:dd HH:mm:ss",
            "yyyy:MM:dd HH:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy.MM.dd HH:mm:ss",
            "yyyy.MM.dd HH:mm",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.f",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm.fff",
            "yyyy-MM-ddTHH:mm.ff",
            "yyyy-MM-ddTHH:mm.f",
            "yyyy-MM-ddTHH:mm",
            "yyyy:MM:dd",
            "yyyy-MM-dd",
            "yyyy-MM",
            "yyyyMMdd", // as used in IPTC data
            "yyyy"
        };

        /// <summary>Attempts to return the specified tag's value as a DateTime.</summary>
        /// <remarks>If the underlying value is a <see cref="string"/>, then attempts will be made to parse it.</remarks>
        /// <returns><c>true</c> if a DateTime was returned, otherwise <c>false</c>.</returns>
        [Pure]
        public static bool TryGetDateTime([NotNull] this Directory directory, int tagType /*, [CanBeNull] TimeZoneInfo timeZone = null*/, out DateTime dateTime)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
            {
                dateTime = default(DateTime);
                return false;
            }

            if (o is DateTime)
            {
                dateTime = (DateTime)o;
                return true;
            }

            var s = o as string;

            if (o is StringValue)
                s = ((StringValue)o).ToString();

            if (s != null)
            {
                if (DateTime.TryParseExact(s, _datePatterns, null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out dateTime))
                    return true;

                dateTime = default(DateTime);
                return false;
            }

            if (o is IConvertible)
            {
                try
                {
                    dateTime = ((IConvertible)o).ToDateTime(null);
                    return true;
                }
                catch (FormatException)
                { }
            }

            dateTime = default(DateTime);
            return false;
        }

        #endregion

        #region Rational

        [Pure]
        public static Rational GetRational([NotNull] this Directory directory, int tagType)
        {
            Rational value;
            if (directory.TryGetRational(tagType, out value))
                return value;

            return ThrowValueNotPossible<Rational>(directory, tagType);
        }

        /// <summary>Returns the specified tag's value as a Rational.</summary>
        /// <remarks>If the value is unset or cannot be converted, <c>null</c> is returned.</remarks>
        [Pure]
        public static bool TryGetRational([NotNull] this Directory directory, int tagType, out Rational value)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
            {
                value = default(Rational);
                return false;
            }

            if (o is Rational)
            {
                value = (Rational)o;
                return true;
            }

            if (o is int)
            {
                value = new Rational((int)o, 1);
                return true;
            }

            if (o is long)
            {
                value = new Rational((long)o, 1);
                return true;
            }

            // NOTE not doing conversions for real number types

            value = default(Rational);
            return false;
        }

        #endregion

        /// <summary>Returns the specified tag's value as an array of Rational.</summary>
        /// <remarks>If the value is unset or cannot be converted, <c>null</c> is returned.</remarks>
        [Pure]
        [CanBeNull]
        public static Rational[] GetRationalArray([NotNull] this Directory directory, int tagType)
        {
            return directory.GetObject(tagType) as Rational[];
        }

        /// <summary>Returns the specified tag's value as a String.</summary>
        /// <remarks>
        /// This value is the 'raw' value.  A more presentable decoding
        /// of this value may be obtained from the corresponding Descriptor.
        /// </remarks>
        /// <returns>
        /// the String representation of the tag's value, or
        /// <c>null</c> if the tag hasn't been defined.
        /// </returns>
        [Pure]
        [CanBeNull]
        public static string GetString([NotNull] this Directory directory, int tagType)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
                return null;

            if (o is Rational)
                return ((Rational)o).ToSimpleString();

            if (o is DateTime)
                return ((DateTime)o).ToString(
                    ((DateTime)o).Kind != DateTimeKind.Unspecified
                        ? "ddd MMM dd HH:mm:ss zzz yyyy"
                        : "ddd MMM dd HH:mm:ss yyyy");

            if (o is bool)
                return ((bool)o) ? "true" : "false";

            // handle arrays of objects and primitives
            if (o is Array)
            {
                Array array = (Array)o;
                var componentType = array.GetType().GetElementType();
                var str = new StringBuilder();

                if (componentType == typeof(float))
                {
                    var vals = (float[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.AppendFormat("{0:0.###}", vals[i]);
                    }
                }
                else if (componentType == typeof(double))
                {
                    var vals = (double[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.AppendFormat("{0:0.###}", vals[i]);
                    }
                }
                else if (componentType == typeof(int))
                {
                    var vals = (int[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else if (componentType == typeof(uint))
                {
                    var vals = (uint[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else if (componentType == typeof(short))
                {
                    var vals = (short[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else if (componentType == typeof(ushort))
                {
                    var vals = (ushort[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else if (componentType == typeof(byte))
                {
                    var vals = (byte[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else if (componentType == typeof(sbyte))
                {
                    var vals = (sbyte[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else if (componentType == typeof(Rational))
                {
                    var vals = (Rational[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else if (componentType == typeof(string))
                {
                    var vals = (string[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else if (componentType.IsByRef)
                {
                    var vals = (object[])array;
                    for (var i = 0; i < vals.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(vals[i]);
                    }
                }
                else
                {
                    for (var i = 0; i < array.Length; i++)
                    {
                        if (i != 0)
                            str.Append(' ');
                        str.Append(array.GetValue(i));
                    }
                }

                return str.ToString();
            }

            if (o is double)
                return ((double)o).ToString("0.###");

            if (o is float)
                return ((float)o).ToString("0.###");

            // Note that several cameras leave trailing spaces (Olympus, Nikon) but this library is intended to show
            // the actual data within the file.  It is not inconceivable that whitespace may be significant here, so we
            // do not trim.  Also, if support is added for writing data back to files, this may cause issues.
            // We leave trimming to the presentation layer.
            return o.ToString();
        }

        [Pure]
        [CanBeNull]
        public static string GetString([NotNull] this Directory directory, int tagType, [NotNull] Encoding encoding)
        {
            var bytes = directory.GetByteArray(tagType);
            return bytes == null
                ? null
                : encoding.GetString(bytes, 0, bytes.Length);
        }

        [Pure]
        public static StringValue GetStringValue([NotNull] this Directory directory, int tagType)
        {
            var o = directory.GetObject(tagType);
            
            if (o is StringValue)
                return (StringValue)o;

            return default(StringValue);
        }

        [Pure]
        [CanBeNull]
        private static IConvertible GetConvertibleObject([NotNull] this Directory directory, int tagType)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
                return null;

            if (o is IConvertible)
                return (IConvertible)o;

            if (o is Array && ((Array)o).Length == 1 && ((Array)o).Rank == 1)
                return ((Array)o).GetValue(0) as IConvertible;

            return null;
        }

        private static T ThrowValueNotPossible<T>([NotNull] Directory directory, int tagType)
        {
            var o = directory.GetObject(tagType);

            if (o == null)
                throw new MetadataException("No value exists for tag " + directory.GetTagName(tagType) + ".");

            throw new MetadataException("Tag " + tagType + " cannot be converted to " + typeof(T).Name + ".  It is of type " + o.GetType() + " with value: " + o + "");
        }
    }

    /// <summary>
    /// Models a key/value pair, where both are non-null <see cref="string"/> objects.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class KeyValuePair
    {
        public KeyValuePair([NotNull] string key, StringValue value)
        {
            _Key = key;
            _Value = value;
        }

        [NotNull]
        public string Key { get { return _Key; } }
        string _Key;

        public StringValue Value { get { return _Value; } }
        StringValue _Value;
    }
}

namespace MetadataExtractor.IO
{
    /// <summary>Base class for reading sequentially through a sequence of data encoded in a byte stream.</summary>
    /// <remarks>
    /// Concrete implementations include:
    /// <list type="bullet">
    ///   <item><see cref="SequentialByteArrayReader"/></item>
    ///   <item><see cref="SequentialStreamReader"/></item>
    /// </list>
    /// By default, the reader operates with Motorola byte order (big endianness).  This can be changed by via
    /// <see cref="IsMotorolaByteOrder"/>.
    /// </remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public abstract class SequentialReader
    {
        protected SequentialReader(bool isMotorolaByteOrder)
        {
            IsMotorolaByteOrder = isMotorolaByteOrder;
        }

        /// <summary>Get and set the byte order of this reader. <c>true</c> by default.</summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><c>true</c> for Motorola (or big) endianness (also known as network byte order), with MSB before LSB.</item>
        ///   <item><c>false</c> for Intel (or little) endianness, with LSB before MSB.</item>
        /// </list>
        /// </remarks>
        /// <value><c>true</c> for Motorola/big endian, <c>false</c> for Intel/little endian</value>
        public bool IsMotorolaByteOrder { get; private set; }

        public abstract long Position { get; }

        public abstract SequentialReader WithByteOrder(bool isMotorolaByteOrder);

        /// <summary>Returns the required number of bytes from the sequence.</summary>
        /// <param name="count">The number of bytes to be returned</param>
        /// <returns>The requested bytes</returns>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public abstract byte[] GetBytes(int count);

        /// <summary>Retrieves bytes, writing them into a caller-provided buffer.</summary>
        /// <param name="buffer">The array to write bytes to.</param>
        /// <param name="offset">The starting position within <paramref name="buffer"/> to write to.</param>
        /// <param name="count">The number of bytes to be written.</param>
        /// <returns>The requested bytes</returns>
        /// <exception cref="System.IO.IOException"/>
        public abstract void GetBytes([NotNull] byte[] buffer, int offset, int count);

        /// <summary>Skips forward in the sequence.</summary>
        /// <remarks>
        /// Skips forward in the sequence. If the sequence ends, an <see cref="System.IO.IOException"/> is thrown.
        /// </remarks>
        /// <param name="n">the number of byte to skip. Must be zero or greater.</param>
        /// <exception cref="System.IO.IOException">the end of the sequence is reached.</exception>
        /// <exception cref="System.IO.IOException">an error occurred reading from the underlying source.</exception>
        public abstract void Skip(long n);

        /// <summary>Skips forward in the sequence, returning a boolean indicating whether the skip succeeded, or whether the sequence ended.</summary>
        /// <param name="n">the number of byte to skip. Must be zero or greater.</param>
        /// <returns>a boolean indicating whether the skip succeeded, or whether the sequence ended.</returns>
        /// <exception cref="System.IO.IOException">an error occurred reading from the underlying source.</exception>
        public abstract bool TrySkip(long n);

        /// <summary>Returns the next unsigned byte from the sequence.</summary>
        /// <returns>the 8 bit int value, between 0 and 255</returns>
        /// <exception cref="System.IO.IOException"/>
        public abstract byte GetByte();

        /// <summary>Returns a signed 8-bit int calculated from the next byte the sequence.</summary>
        /// <returns>the 8 bit int value, between 0x00 and 0xFF</returns>
        /// <exception cref="System.IO.IOException"/>
        public sbyte GetSByte() { return unchecked((sbyte)GetByte()); }

        /// <summary>Returns an unsigned 16-bit int calculated from the next two bytes of the sequence.</summary>
        /// <returns>the 16 bit int value, between 0x0000 and 0xFFFF</returns>
        /// <exception cref="System.IO.IOException"/>
        public ushort GetUInt16()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return (ushort)
                    ((ushort)GetByte() << 8 |
                     (ushort)GetByte());
            }
            // Intel ordering - LSB first
            return (ushort)
                ((ushort)GetByte() |
                 (ushort)GetByte() << 8);
        }

        /// <summary>Returns a signed 16-bit int calculated from two bytes of data (MSB, LSB).</summary>
        /// <returns>the 16 bit int value, between 0x0000 and 0xFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request</exception>
        public short GetInt16()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return unchecked((short)
                    ((ushort)GetByte() << 8 |
                     (ushort)GetByte()));
            }
            // Intel ordering - LSB first
            return unchecked((short)
                ((ushort)GetByte() |
                 (ushort)GetByte() << 8));
        }

        /// <summary>Get a 32-bit unsigned integer from the buffer, returning it as a long.</summary>
        /// <returns>the unsigned 32-bit int value as a long, between 0x00000000 and 0xFFFFFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request</exception>
        public uint GetUInt32()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return
                    ((uint)GetByte() << 24 |
                     (uint)GetByte() << 16 |
                     (uint)GetByte() << 8  |
                     (uint)GetByte());
            }
            // Intel ordering - LSB first (little endian)
            return
                ((uint)GetByte()       |
                 (uint)GetByte() << 8  |
                 (uint)GetByte() << 16 |
                 (uint)GetByte() << 24);
        }

        /// <summary>Returns a signed 32-bit integer from four bytes of data.</summary>
        /// <returns>the signed 32 bit int value, between 0x00000000 and 0xFFFFFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request</exception>
        public int GetInt32()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return unchecked((int)(
                    (uint)GetByte() << 24 |
                    (uint)GetByte() << 16 |
                    (uint)GetByte() << 8  |
                    (uint)GetByte()));
            }
            // Intel ordering - LSB first (little endian)
            return unchecked((int)(
                (uint)GetByte()       |
                (uint)GetByte() <<  8 |
                (uint)GetByte() << 16 |
                (uint)GetByte() << 24));
        }

        /// <summary>Get a signed 64-bit integer from the buffer.</summary>
        /// <returns>the 64 bit int value, between 0x0000000000000000 and 0xFFFFFFFFFFFFFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request</exception>
        public long GetInt64()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return unchecked((long)(
                    (ulong)GetByte() << 56 |
                    (ulong)GetByte() << 48 |
                    (ulong)GetByte() << 40 |
                    (ulong)GetByte() << 32 |
                    (ulong)GetByte() << 24 |
                    (ulong)GetByte() << 16 |
                    (ulong)GetByte() << 8  |
                    (ulong)GetByte()));
            }
            // Intel ordering - LSB first
            return unchecked((long)(
                (ulong)GetByte()       |
                (ulong)GetByte() << 8  |
                (ulong)GetByte() << 16 |
                (ulong)GetByte() << 24 |
                (ulong)GetByte() << 32 |
                (ulong)GetByte() << 40 |
                (ulong)GetByte() << 48 |
                (ulong)GetByte() << 56));
        }

        /// <summary>Get an usigned 64-bit integer from the buffer.</summary>
        /// <returns>the unsigned 64 bit int value, between 0x0000000000000000 and 0xFFFFFFFFFFFFFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request</exception>
        public ulong GetUInt64()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return
                    (ulong)GetByte() << 56 |
                    (ulong)GetByte() << 48 |
                    (ulong)GetByte() << 40 |
                    (ulong)GetByte() << 32 |
                    (ulong)GetByte() << 24 |
                    (ulong)GetByte() << 16 |
                    (ulong)GetByte() << 8  |
                    (ulong)GetByte();
            }
            // Intel ordering - LSB first
            return
                (ulong)GetByte()       |
                (ulong)GetByte() << 8  |
                (ulong)GetByte() << 16 |
                (ulong)GetByte() << 24 |
                (ulong)GetByte() << 32 |
                (ulong)GetByte() << 40 |
                (ulong)GetByte() << 48 |
                (ulong)GetByte() << 56;
        }

        /// <summary>Gets a s15.16 fixed point float from the buffer.</summary>
        /// <remarks>
        /// Gets a s15.16 fixed point float from the buffer.
        /// <para />
        /// This particular fixed point encoding has one sign bit, 15 numerator bits and 16 denominator bits.
        /// </remarks>
        /// <returns>the floating point value</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request</exception>
        public float GetS15Fixed16()
        {
            if (IsMotorolaByteOrder)
            {
                float res = GetByte() << 8 | GetByte();
                var d = GetByte() << 8 | GetByte();
                return (float)(res + d / 65536.0);
            }
            else
            {
                // this particular branch is untested
                var d = GetByte() | GetByte() << 8;
                float res = GetByte() | GetByte() << 8;
                return (float)(res + d / 65536.0);
            }
        }

        /// <exception cref="System.IO.IOException"/>
        public float GetFloat32() { return BitConverter.ToSingle(BitConverter.GetBytes(GetInt32()), 0); }

        /// <exception cref="System.IO.IOException"/>
        public double GetDouble64() { return BitConverter.Int64BitsToDouble(GetInt64()); }

        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public string GetString(int bytesRequested, [NotNull] Encoding encoding)
        {
            var bytes = GetBytes(bytesRequested);
            return encoding.GetString(bytes, 0, bytes.Length);
        }

        public StringValue GetStringValue(int bytesRequested, Encoding encoding = null)
        {
            return new StringValue(GetBytes(bytesRequested), encoding);
        }

        /// <summary>
        /// Creates a <see cref="String"/> from the stream, ending where <c>byte=='\0'</c> or where <c>length==maxLength</c>.
        /// </summary>
        /// <param name="maxLengthBytes">
        /// The maximum number of bytes to read.  If a <c>\0</c> byte is not reached within this limit,
        /// reading will stop and the string will be truncated to this length.
        /// </param>
        /// <param name="encoding">An optional string encoding. If none is provided, <see cref="Encoding.UTF8"/> is used.</param>
        /// <returns>The read <see cref="string"/></returns>
        /// <exception cref="System.IO.IOException">The buffer does not contain enough bytes to satisfy this request.</exception>
        [NotNull]
        public string GetNullTerminatedString(int maxLengthBytes, Encoding encoding = null)
        {
            var bytes = GetNullTerminatedBytes(maxLengthBytes);

            return (encoding ?? Encoding.UTF8).GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Creates a <see cref="StringValue"/> from the stream, ending where <c>byte=='\0'</c> or where <c>length==maxLength</c>.
        /// </summary>
        /// <param name="maxLengthBytes">
        /// The maximum number of bytes to read.  If a <c>\0</c> byte is not reached within this limit,
        /// reading will stop and the string will be truncated to this length.
        /// </param>
        /// <param name="encoding">An optional string encoding to use when interpreting bytes.</param>
        /// <returns>The read string as a <see cref="StringValue"/>, excluding the null terminator.</returns>
        /// <exception cref="System.IO.IOException">The buffer does not contain enough bytes to satisfy this request.</exception>
        public StringValue GetNullTerminatedStringValue(int maxLengthBytes, Encoding encoding = null)
        {
            var bytes = GetNullTerminatedBytes(maxLengthBytes);

            return new StringValue(bytes, encoding);
        }

        /// <summary>
        /// Returns the sequence of bytes punctuated by a <c>\0</c> value.
        /// </summary>
        /// <param name="maxLengthBytes">
        /// The maximum number of bytes to read.  If a <c>\0</c> byte is not reached within this limit,
        /// the returned array will be <paramref name="maxLengthBytes"/> long.
        /// </param>
        /// <returns>The read byte array, excluding the null terminator.</returns>
        /// <exception cref="System.IO.IOException">The buffer does not contain enough bytes to satisfy this request.</exception>
        [NotNull]
        public byte[] GetNullTerminatedBytes(int maxLengthBytes)
        {
            var buffer = new byte[maxLengthBytes];

            // Count the number of non-null bytes
            var length = 0;
            while (length < buffer.Length && (buffer[length] = GetByte()) != 0)
                length++;

            if (length == maxLengthBytes)
                return buffer;

            var bytes = new byte[length];
            if (length > 0)
                Array.Copy(buffer, bytes, length);
            return bytes;
        }

        /// <summary>
        /// Returns true in case the stream supports length checking and distance to the end of the stream is less then number of bytes in parameter.
        /// Otherwise false.
        /// </summary>
        /// <param name="numberOfBytes"></param>
        /// <returns>True if we going to have an exception while reading next numberOfBytes bytes from the stream</returns>
        public virtual bool IsCloserToEnd(long numberOfBytes)
        {
            return false;
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    public class SequentialStreamReader : SequentialReader
    {
        [NotNull]
        private readonly Stream _stream;

        public override long Position { get { return _stream.Position; } }

        public SequentialStreamReader([NotNull] Stream stream, bool isMotorolaByteOrder = true)
            : base(isMotorolaByteOrder)
        {
            if ((_stream = stream) == null) throw new ArgumentNullException("stream");
        }

        public override byte GetByte()
        {
            var value = _stream.ReadByte();
            if (value == -1)
                throw new IOException("End of data reached.");

            return unchecked((byte)value);
        }

        public override SequentialReader WithByteOrder(bool isMotorolaByteOrder) { return isMotorolaByteOrder == IsMotorolaByteOrder ? this : new SequentialStreamReader(_stream, isMotorolaByteOrder); }

        public override byte[] GetBytes(int count)
        {
            var bytes = new byte[count];
            GetBytes(bytes, 0, count);
            return bytes;
        }

        public override void GetBytes(byte[] buffer, int offset, int count)
        {
            var totalBytesRead = 0;
            while (totalBytesRead != count)
            {
                var bytesRead = _stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                    throw new IOException("End of data reached.");
                totalBytesRead += bytesRead;
                Debug.Assert(totalBytesRead <= count);
            }
        }

        public override void Skip(long n)
        {
            if (n < 0)
                throw new ArgumentException("n must be zero or greater.");

            if (_stream.Position + n > _stream.Length)
                throw new IOException("Unable to skip past of end of file");

            _stream.Seek(n, SeekOrigin.Current);
        }

        public override bool TrySkip(long n)
        {
            try
            {
                Skip(n);
                return true;
            }
            catch (IOException)
            {
                // Stream ended, or error reading from underlying source
                return false;
            }
        }

        public override bool IsCloserToEnd(long numberOfBytes)
        {
            return _stream.Position + numberOfBytes > _stream.Length;
        }
    }

    /// <summary>
    /// Thrown when the index provided to an <see cref="IndexedReader"/> is invalid.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class BufferBoundsException : IOException
    {
        public BufferBoundsException(int index, int bytesRequested, long bufferLength)
            : base(GetMessage(index, bytesRequested, bufferLength))
        {
        }

        public BufferBoundsException(string message)
            : base(message)
        {
        }

        [NotNull]
        private static string GetMessage(int index, int bytesRequested, long bufferLength)
        {
            if (index < 0)
                return "Attempt to read from buffer using a negative index (" + index + ")";

            if (bytesRequested < 0)
                return "Number of requested bytes cannot be negative (" + bytesRequested + ")";

            if (index + (long)bytesRequested - 1L > int.MaxValue)
                return "Number of requested bytes summed with starting index exceed maximum range of signed 32 bit integers (requested index: " + index + ", requested count: " + bytesRequested + ")";

            return "Attempt to read from beyond end of underlying data source (requested index: " + index + ", requested count: " + bytesRequested + ", max index: " + (bufferLength - 1) + ")";
        }

#if !NETSTANDARD1_3
        protected BufferBoundsException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    /// Reads values of various data types from a byte array, accessed by index.
    /// </summary>
    /// <remarks>
    /// By default, the reader operates with Motorola byte order (big endianness).  This can be changed by calling
    /// <see cref="IndexedReader.IsMotorolaByteOrder"/>.
    /// </remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public class ByteArrayReader : IndexedReader
    {
        [NotNull]
        private readonly byte[] _buffer;
        private readonly int _baseOffset;

        public ByteArrayReader([NotNull] byte[] buffer, int baseOffset = 0, bool isMotorolaByteOrder = true)
            : base(isMotorolaByteOrder)
        {
            if (baseOffset < 0)
                throw new ArgumentOutOfRangeException("baseOffset", "Must be zero or greater.");

            if ((_buffer = buffer) == null) throw new ArgumentNullException("buffer");
            _baseOffset = baseOffset;
        }

        public override IndexedReader WithByteOrder(bool isMotorolaByteOrder) { return isMotorolaByteOrder == IsMotorolaByteOrder ? this : new ByteArrayReader(_buffer, _baseOffset, isMotorolaByteOrder); }

        public override IndexedReader WithShiftedBaseOffset(int shift) { return shift == 0 ? this : new ByteArrayReader(_buffer, _baseOffset + shift, IsMotorolaByteOrder); }

        public override int ToUnshiftedOffset(int localOffset) { return localOffset + _baseOffset; }

        public override long Length { get { return _buffer.Length - _baseOffset; }  }

        public override byte GetByte(int index)
        {
            ValidateIndex(index, 1);
            return _buffer[index + _baseOffset];
        }

        protected override void ValidateIndex(int index, int bytesRequested)
        {
            if (!IsValidIndex(index, bytesRequested))
                throw new BufferBoundsException(ToUnshiftedOffset(index), bytesRequested, _buffer.Length);
        }

        protected override bool IsValidIndex(int index, int bytesRequested)
        {
            return
                bytesRequested >= 0 &&
                index >= 0 &&
                index + (long)bytesRequested - 1L < Length;
        }

        public override byte[] GetBytes(int index, int count)
        {
            ValidateIndex(index, count);

            var bytes = new byte[count];
            Array.Copy(_buffer, index + _baseOffset, bytes, 0, count);
            return bytes;
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    public class SequentialByteArrayReader : SequentialReader
    {
        [NotNull]
        private readonly byte[] _bytes;

        private int _index;

        public override long Position { get { return _index; } }

        public SequentialByteArrayReader([NotNull] byte[] bytes, int baseIndex = 0, bool isMotorolaByteOrder = true)
            : base(isMotorolaByteOrder)
        {
            if ((_bytes = bytes) == null) throw new ArgumentNullException("bytes");
            _index = baseIndex;
        }

        public override byte GetByte()
        {
            if (_index >= _bytes.Length)
                throw new IOException("End of data reached.");

            return _bytes[_index++];
        }

        public override SequentialReader WithByteOrder(bool isMotorolaByteOrder) { return isMotorolaByteOrder == IsMotorolaByteOrder ? this : new SequentialByteArrayReader(_bytes, _index, isMotorolaByteOrder); }

        public override byte[] GetBytes(int count)
        {
            if (_index + count > _bytes.Length)
                throw new IOException("End of data reached.");

            var bytes = new byte[count];
            Array.Copy(_bytes, _index, bytes, 0, count);
            _index += count;
            return bytes;
        }

        public override void GetBytes(byte[] buffer, int offset, int count)
        {
            if (_index + count > _bytes.Length)
                throw new IOException("End of data reached.");

            Array.Copy(_bytes, _index, buffer, offset, count);
            _index += count;
        }

        public override void Skip(long n)
        {
            if (n < 0)
                throw new ArgumentException("n must be zero or greater.");

            if (_index + n > _bytes.Length)
                throw new IOException("End of data reached.");

            _index += unchecked((int)n);
        }

        public override bool TrySkip(long n)
        {
            if (n < 0)
                throw new ArgumentException("n must be zero or greater.");

            _index += unchecked((int)n);

            if (_index > _bytes.Length)
            {
                _index = _bytes.Length;
                return false;
            }

            return true;
        }

		public override bool IsCloserToEnd(long numberOfBytes)
		{
			return _index + numberOfBytes > _bytes.Length;
		}
	}

    /// <summary>Base class for random access data reading operations of common data types.</summary>
    /// <remarks>
    /// Concrete implementations include:
    /// <list type="bullet">
    ///   <item><see cref="ByteArrayReader"/></item>
    ///   <item><see cref="IndexedSeekingReader"/></item>
    ///   <item><see cref="IndexedCapturingReader"/></item>
    /// </list>
    /// By default, the reader operates with Motorola byte order (big endianness).  This can be changed by via
    /// <see cref="IsMotorolaByteOrder"/>.
    /// </remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public abstract class IndexedReader
    {
        protected IndexedReader(bool isMotorolaByteOrder)
        {
            _IsMotorolaByteOrder = isMotorolaByteOrder;
        }

        /// <summary>Get the byte order of this reader.</summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><c>true</c> for Motorola (or big) endianness (also known as network byte order), with MSB before LSB.</item>
        ///   <item><c>false</c> for Intel (or little) endianness, with LSB before MSB.</item>
        /// </list>
        /// </remarks>
        public bool IsMotorolaByteOrder { get { return _IsMotorolaByteOrder; } }
        bool _IsMotorolaByteOrder;

        public abstract IndexedReader WithByteOrder(bool isMotorolaByteOrder);

        public abstract IndexedReader WithShiftedBaseOffset(int shift);

        public abstract int ToUnshiftedOffset(int localOffset);

        /// <summary>Gets the byte value at the specified byte <c>index</c>.</summary>
        /// <remarks>
        /// Implementations must validate <paramref name="index"/> by calling <see cref="ValidateIndex"/>.
        /// </remarks>
        /// <param name="index">The index from which to read the byte</param>
        /// <returns>The read byte value</returns>
        /// <exception cref="System.ArgumentException"><c>index</c> is negative</exception>
        /// <exception cref="BufferBoundsException">if the requested byte is beyond the end of the underlying data source</exception>
        /// <exception cref="System.IO.IOException">if the byte is unable to be read</exception>
        public abstract byte GetByte(int index);

        /// <summary>Returns the required number of bytes from the specified index from the underlying source.</summary>
        /// <param name="index">The index from which the bytes begins in the underlying source</param>
        /// <param name="count">The number of bytes to be returned</param>
        /// <returns>The requested bytes</returns>
        /// <exception cref="System.ArgumentException"><c>index</c> or <c>count</c> are negative</exception>
        /// <exception cref="BufferBoundsException">if the requested bytes extend beyond the end of the underlying data source</exception>
        /// <exception cref="System.IO.IOException">if the byte is unable to be read</exception>
        [NotNull]
        public abstract byte[] GetBytes(int index, int count);

        /// <summary>
        /// Ensures that the buffered bytes extend to cover the specified index. If not, an attempt is made
        /// to read to that point.
        /// </summary>
        /// <remarks>
        /// If the stream ends before the point is reached, a <see cref="BufferBoundsException"/> is raised.
        /// </remarks>
        /// <param name="index">the index from which the required bytes start</param>
        /// <param name="bytesRequested">the number of bytes which are required</param>
        /// <exception cref="System.IO.IOException">if the stream ends before the required number of bytes are acquired</exception>
        protected abstract void ValidateIndex(int index, int bytesRequested);

        /// <exception cref="System.IO.IOException"/>
        protected abstract bool IsValidIndex(int index, int bytesRequested);

        /// <summary>Returns the length of the data source in bytes.</summary>
        /// <remarks>
        /// This is a simple operation for implementations (such as <see cref="IndexedSeekingReader"/> and
        /// <see cref="ByteArrayReader"/>) that have the entire data source available.
        /// <para />
        /// Users of this method must be aware that sequentially accessed implementations such as
        /// <see cref="IndexedCapturingReader"/>
        /// will have to read and buffer the entire data source in order to determine the length.
        /// </remarks>
        /// <value>the length of the data source, in bytes.</value>
        /// <exception cref="System.IO.IOException"/>
        public abstract long Length { get; }

        /// <summary>Gets whether a bit at a specific index is set or not.</summary>
        /// <param name="index">the number of bits at which to test</param>
        /// <returns>true if the bit is set, otherwise false</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public bool GetBit(int index)
        {
            var byteIndex = index / 8;
            var bitIndex = index % 8;
            ValidateIndex(byteIndex, 1);
            var b = GetByte(byteIndex);
            return ((b >> bitIndex) & 1) == 1;
        }

        /// <summary>Returns a signed 8-bit int calculated from one byte of data at the specified index.</summary>
        /// <param name="index">position within the data buffer to read byte</param>
        /// <returns>the 8 bit signed byte value</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public sbyte GetSByte(int index)
        {
            ValidateIndex(index, 1);
            return unchecked((sbyte)GetByte(index));
        }

        /// <summary>Returns an unsigned 16-bit int calculated from two bytes of data at the specified index.</summary>
        /// <param name="index">position within the data buffer to read first byte</param>
        /// <returns>the 16 bit int value, between 0x0000 and 0xFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public ushort GetUInt16(int index)
        {
            ValidateIndex(index, 2);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return (ushort)
                    (GetByte(index    ) << 8 |
                     GetByte(index + 1));
            }
            // Intel ordering - LSB first
            return (ushort)
                (GetByte(index + 1) << 8 |
                 GetByte(index    ));
        }

        /// <summary>Returns a signed 16-bit int calculated from two bytes of data at the specified index (MSB, LSB).</summary>
        /// <param name="index">position within the data buffer to read first byte</param>
        /// <returns>the 16 bit int value, between 0x0000 and 0xFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public short GetInt16(int index)
        {
            ValidateIndex(index, 2);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return (short)
                    (GetByte(index    ) << 8 |
                     GetByte(index + 1));
            }
            // Intel ordering - LSB first
            return (short)
                (GetByte(index + 1) << 8 |
                 GetByte(index));
        }

        /// <summary>Get a 24-bit unsigned integer from the buffer, returning it as an int.</summary>
        /// <param name="index">position within the data buffer to read first byte</param>
        /// <returns>the unsigned 24-bit int value as a long, between 0x00000000 and 0x00FFFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public int GetInt24(int index)
        {
            ValidateIndex(index, 3);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return
                    GetByte(index    ) << 16 |
                    GetByte(index + 1)  << 8 |
                    GetByte(index + 2);
            }
            // Intel ordering - LSB first (little endian)
            return
                GetByte(index + 2) << 16 |
                GetByte(index + 1) <<  8 |
                GetByte(index    );
        }

        /// <summary>Get a 32-bit unsigned integer from the buffer, returning it as a long.</summary>
        /// <param name="index">position within the data buffer to read first byte</param>
        /// <returns>the unsigned 32-bit int value as a long, between 0x00000000 and 0xFFFFFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public uint GetUInt32(int index)
        {
            ValidateIndex(index, 4);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return (uint)
                    (GetByte(index    ) << 24 |
                     GetByte(index + 1) << 16 |
                     GetByte(index + 2) <<  8 |
                     GetByte(index + 3));
            }
            // Intel ordering - LSB first (little endian)
            return (uint)
                (GetByte(index + 3) << 24 |
                 GetByte(index + 2) << 16 |
                 GetByte(index + 1) <<  8 |
                 GetByte(index    ));
        }

        /// <summary>Returns a signed 32-bit integer from four bytes of data at the specified index the buffer.</summary>
        /// <param name="index">position within the data buffer to read first byte</param>
        /// <returns>the signed 32 bit int value, between 0x00000000 and 0xFFFFFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public int GetInt32(int index)
        {
            ValidateIndex(index, 4);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return
                    GetByte(index    ) << 24 |
                    GetByte(index + 1) << 16 |
                    GetByte(index + 2) <<  8 |
                    GetByte(index + 3);
            }
            // Intel ordering - LSB first (little endian)
            return
                GetByte(index + 3) << 24 |
                GetByte(index + 2) << 16 |
                GetByte(index + 1) <<  8 |
                GetByte(index    );
        }

        /// <summary>Get a signed 64-bit integer from the buffer.</summary>
        /// <param name="index">position within the data buffer to read first byte</param>
        /// <returns>the 64 bit int value, between 0x0000000000000000 and 0xFFFFFFFFFFFFFFFF</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public long GetInt64(int index)
        {
            ValidateIndex(index, 8);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return
                    (long)GetByte(index    ) << 56 |
                    (long)GetByte(index + 1) << 48 |
                    (long)GetByte(index + 2) << 40 |
                    (long)GetByte(index + 3) << 32 |
                    (long)GetByte(index + 4) << 24 |
                    (long)GetByte(index + 5) << 16 |
                    (long)GetByte(index + 6) <<  8 |
                          GetByte(index + 7);
            }
            // Intel ordering - LSB first
            return
                (long)GetByte(index + 7) << 56 |
                (long)GetByte(index + 6) << 48 |
                (long)GetByte(index + 5) << 40 |
                (long)GetByte(index + 4) << 32 |
                (long)GetByte(index + 3) << 24 |
                (long)GetByte(index + 2) << 16 |
                (long)GetByte(index + 1) <<  8 |
                      GetByte(index    );
        }

        /// <summary>Gets a s15.16 fixed point float from the buffer.</summary>
        /// <remarks>
        /// This particular fixed point encoding has one sign bit, 15 numerator bits and 16 denominator bits.
        /// </remarks>
        /// <returns>the floating point value</returns>
        /// <exception cref="System.IO.IOException">the buffer does not contain enough bytes to service the request, or index is negative</exception>
        public float GetS15Fixed16(int index)
        {
            ValidateIndex(index, 4);
            if (IsMotorolaByteOrder)
            {
                float res = GetByte(index) << 8 | GetByte(index + 1);
                var d = GetByte(index + 2) << 8 | GetByte(index + 3);
                return (float)(res + d / 65536.0);
            }
            else
            {
                // this particular branch is untested
                var d = GetByte(index + 1) << 8 | GetByte(index);
                float res = GetByte(index + 3) << 8 | GetByte(index + 2);
                return (float)(res + d / 65536.0);
            }
        }

        /// <exception cref="System.IO.IOException"/>
        public float GetFloat32(int index) { return BitConverter.ToSingle(BitConverter.GetBytes(GetInt32(index)), 0); }

        /// <exception cref="System.IO.IOException"/>
        public double GetDouble64(int index) { return BitConverter.Int64BitsToDouble(GetInt64(index)); }

        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public string GetString(int index, int bytesRequested, [NotNull] Encoding encoding)
        {
            var bytes = GetBytes(index, bytesRequested);
            return encoding.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Creates a string starting at the specified index, and ending where either <c>byte=='\0'</c> or
        /// <c>length==maxLength</c>.
        /// </summary>
        /// <param name="index">The index within the buffer at which to start reading the string.</param>
        /// <param name="maxLengthBytes">
        /// The maximum number of bytes to read.  If a zero-byte is not reached within this limit,
        /// reading will stop and the string will be truncated to this length.
        /// </param>
        /// <param name="encoding">An optional string encoding. If none is provided, <see cref="Encoding.UTF8"/> is used.</param>
        /// <returns>The read <see cref="string"/></returns>
        /// <exception cref="System.IO.IOException">The buffer does not contain enough bytes to satisfy this request.</exception>
        [NotNull]
        public string GetNullTerminatedString(int index, int maxLengthBytes, Encoding encoding = null)
        {
            var bytes = GetNullTerminatedBytes(index, maxLengthBytes);

            return (encoding ?? Encoding.UTF8).GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Creates a string starting at the specified index, and ending where either <c>byte=='\0'</c> or
        /// <c>length==maxLength</c>.
        /// </summary>
        /// <param name="index">The index within the buffer at which to start reading the string.</param>
        /// <param name="maxLengthBytes">
        /// The maximum number of bytes to read.  If a zero-byte is not reached within this limit,
        /// reading will stop and the string will be truncated to this length.
        /// </param>
        /// <param name="encoding">An optional string encoding to use when interpreting bytes.</param>
        /// <returns>The read <see cref="StringValue"/></returns>
        /// <exception cref="System.IO.IOException">The buffer does not contain enough bytes to satisfy this request.</exception>
        public StringValue GetNullTerminatedStringValue(int index, int maxLengthBytes, Encoding encoding = null)
        {
            var bytes = GetNullTerminatedBytes(index, maxLengthBytes);

            return new StringValue(bytes, encoding);
        }

        /// <summary>
        /// Returns the sequence of bytes punctuated by a <c>\0</c> value.
        /// </summary>
        /// <param name="index">The index to start reading from.</param>
        /// <param name="maxLengthBytes">
        /// The maximum number of bytes to read.  If a <c>\0</c> byte is not reached within this limit,
        /// the returned array will be <paramref name="maxLengthBytes"/> long.
        /// </param>
        /// <returns>The read byte array.</returns>
        /// <exception cref="System.IO.IOException">The buffer does not contain enough bytes to satisfy this request.</exception>
        [NotNull]
        public byte[] GetNullTerminatedBytes(int index, int maxLengthBytes)
        {
            var buffer = GetBytes(index, maxLengthBytes);

            // Count the number of non-null bytes
            var length = 0;
            while (length < buffer.Length && buffer[length] != 0)
                length++;

            if (length == maxLengthBytes)
                return buffer;

            var bytes = new byte[length];
            if (length > 0)
                Array.Copy(buffer, 0, bytes, 0, length);
            return bytes;
        }
    }

    /// <summary>
    /// Provides methods to read data types from a <see cref="Stream"/> by indexing into the data.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public class IndexedSeekingReader : IndexedReader
    {
        [NotNull]
        private readonly Stream _stream;

        private readonly int _baseOffset;

        public IndexedSeekingReader([NotNull] Stream stream, int baseOffset = 0, bool isMotorolaByteOrder = true)
            : base(isMotorolaByteOrder)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanSeek)
                throw new ArgumentException("Must be capable of seeking.", "stream");
            if (baseOffset < 0)
                throw new ArgumentOutOfRangeException("baseOffset", "Must be zero or greater.");

            var actualLength = stream.Length;
            var availableLength = actualLength - baseOffset;

            if (availableLength < 0)
                throw new ArgumentOutOfRangeException("baseOffset", "Cannot be greater than the stream's length.");

            _stream = stream;
            _baseOffset = baseOffset;
            _length = availableLength;
        }

        public override IndexedReader WithByteOrder(bool isMotorolaByteOrder) { return isMotorolaByteOrder == IsMotorolaByteOrder ? this : new IndexedSeekingReader(_stream, _baseOffset, isMotorolaByteOrder); }

        public override IndexedReader WithShiftedBaseOffset(int shift) { return shift == 0 ? this : new IndexedSeekingReader(_stream, _baseOffset + shift, IsMotorolaByteOrder); }

        public override int ToUnshiftedOffset(int localOffset) { return localOffset + _baseOffset; }

        public override long Length { get { return _length; } }
        long _length;

        public override byte GetByte(int index)
        {
            ValidateIndex(index, 1);

            if (index + _baseOffset != _stream.Position)
                Seek(index);

            var b = _stream.ReadByte();

            if (b < 0)
                throw new BufferBoundsException("Unexpected end of file encountered.");

            return unchecked((byte)b);
        }

        public override byte[] GetBytes(int index, int count)
        {
            ValidateIndex(index, count);

            if (index + _baseOffset != _stream.Position)
                Seek(index);

            var bytes = new byte[count];
            var bytesRead = _stream.Read(bytes, 0, count);

            if (bytesRead != count)
                throw new BufferBoundsException("Unexpected end of file encountered.");

            return bytes;
        }

        private void Seek(int index)
        {
            var streamIndex = index + _baseOffset;
            if (streamIndex == _stream.Position)
                return;

            _stream.Seek(streamIndex, SeekOrigin.Begin);
        }

        protected override bool IsValidIndex(int index, int bytesRequested)
        {
            return
                bytesRequested >= 0 &&
                index >= 0 &&
                index + (long)bytesRequested - 1L < Length;
        }

        protected override void ValidateIndex(int index, int bytesRequested)
        {
            if (!IsValidIndex(index, bytesRequested))
                throw new BufferBoundsException(ToUnshiftedOffset(index), bytesRequested, _stream.Length);
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class IndexedCapturingReader : IndexedReader
    {
        private const int DefaultChunkLength = 2 * 1024;

        [NotNull]
        private readonly Stream _stream;
        private readonly int _chunkLength;
        private readonly List<byte[]> _chunks = new List<byte[]>();
        private bool _isStreamFinished;
        private int _streamLength;
        private bool _streamLengthThrewException;

        public IndexedCapturingReader([NotNull] Stream stream, int chunkLength = DefaultChunkLength, bool isMotorolaByteOrder = true)
            : base(isMotorolaByteOrder)
        {
            if (chunkLength <= 0)
                throw new ArgumentOutOfRangeException("chunkLength", "Must be greater than zero.");

            _chunkLength = chunkLength;
            if ((_stream = stream) == null) throw new ArgumentNullException("stream");
        }

        /// <summary>
        /// Returns the length of the data stream this reader is reading from.
        /// </summary>
        /// <remarks>
        /// If the underlying stream's <see cref="Stream.Length"/> property does not throw <see cref="NotSupportedException"/> then it can be used directly.
        /// However if it does throw, then this class has no alternative but to reads to the end of the stream in order to determine the total number of bytes.
        /// <para />
        /// In general, this is not a good idea for this implementation of <see cref="IndexedReader"/>.
        /// </remarks>
        /// <value>The length of the data source, in bytes.</value>
        /// <exception cref="BufferBoundsException"/>
        public override long Length
        {
            get
            {
                if (!_streamLengthThrewException)
                {
                    try
                    {
                        return _stream.Length;
                    }
                    catch (NotSupportedException)
                    {
                        _streamLengthThrewException = true;
                    }
                }

                IsValidIndex(int.MaxValue, 1);
                Debug.Assert(_isStreamFinished);
                return _streamLength;
            }
        }

        /// <summary>Ensures that the buffered bytes extend to cover the specified index. If not, an attempt is made
        /// to read to that point.</summary>
        /// <remarks>If the stream ends before the point is reached, a <see cref="BufferBoundsException"/> is raised.</remarks>
        /// <param name="index">the index from which the required bytes start</param>
        /// <param name="bytesRequested">the number of bytes which are required</param>
        /// <exception cref="BufferBoundsException">if the stream ends before the required number of bytes are acquired</exception>
        protected override void ValidateIndex(int index, int bytesRequested)
        {
            if (!IsValidIndex(index, bytesRequested))
            {
                if (index < 0)
                    throw new BufferBoundsException("Attempt to read from buffer using a negative index (" + index + ")");
                if (bytesRequested < 0)
                    throw new BufferBoundsException("Number of requested bytes must be zero or greater");
                if ((long)index + bytesRequested - 1 > int.MaxValue)
                    throw new BufferBoundsException("Number of requested bytes summed with starting index exceed maximum range of signed 32 bit integers (requested index: " + index + ", requested count: " + bytesRequested + ")");

                Debug.Assert(_isStreamFinished);
                // TODO test that can continue using an instance of this type after this exception
                throw new BufferBoundsException(ToUnshiftedOffset(index), bytesRequested, _streamLength);
            }
        }

        protected override bool IsValidIndex(int index, int bytesRequested)
        {
            if (index < 0 || bytesRequested < 0)
                return false;

            var endIndexLong = (long)index + bytesRequested - 1;
            if (endIndexLong > int.MaxValue)
                return false;

            var endIndex = (int)endIndexLong;
            if (_isStreamFinished)
                return endIndex < _streamLength;

            var chunkIndex = endIndex / _chunkLength;

            while (chunkIndex >= _chunks.Count)
            {
                Debug.Assert(!_isStreamFinished);

                var chunk = new byte[_chunkLength];
                var totalBytesRead = 0;
                while (!_isStreamFinished && totalBytesRead != _chunkLength)
                {
                    var bytesRead = _stream.Read(chunk, totalBytesRead, _chunkLength - totalBytesRead);

                    if (bytesRead == 0)
                    {
                        // the stream has ended, which may be ok
                        _isStreamFinished = true;
                        _streamLength = _chunks.Count * _chunkLength + totalBytesRead;
                        // check we have enough bytes for the requested index
                        if (endIndex >= _streamLength)
                        {
                            _chunks.Add(chunk);
                            return false;
                        }
                    }
                    else
                    {
                        totalBytesRead += bytesRead;
                    }
                }

                _chunks.Add(chunk);
            }

            return true;
        }

        public override int ToUnshiftedOffset(int localOffset) { return localOffset; }

        public override byte GetByte(int index)
        {
            ValidateIndex(index, 1);

            var chunkIndex = index / _chunkLength;
            var innerIndex = index % _chunkLength;
            var chunk = _chunks[chunkIndex];
            return chunk[innerIndex];
        }

        public override byte[] GetBytes(int index, int count)
        {
            ValidateIndex(index, count);

            var bytes = new byte[count];
            var remaining = count;
            var fromIndex = index;
            var toIndex = 0;
            while (remaining != 0)
            {
                var fromChunkIndex = fromIndex / _chunkLength;
                var fromInnerIndex = fromIndex % _chunkLength;
                var length = Math.Min(remaining, _chunkLength - fromInnerIndex);
                var chunk = _chunks[fromChunkIndex];
                Array.Copy(chunk, fromInnerIndex, bytes, toIndex, length);
                remaining -= length;
                fromIndex += length;
                toIndex += length;
            }
            return bytes;
        }

        public override IndexedReader WithByteOrder(bool isMotorolaByteOrder) { return isMotorolaByteOrder == IsMotorolaByteOrder ? (IndexedReader)this : new ShiftedIndexedCapturingReader(this, 0, isMotorolaByteOrder); }

        public override IndexedReader WithShiftedBaseOffset(int shift) { return shift == 0 ? (IndexedReader)this : new ShiftedIndexedCapturingReader(this, shift, IsMotorolaByteOrder); }

        private sealed class ShiftedIndexedCapturingReader : IndexedReader
        {
            private readonly IndexedCapturingReader _baseReader;
            private readonly int _baseOffset;

            public ShiftedIndexedCapturingReader(IndexedCapturingReader baseReader, int baseOffset, bool isMotorolaByteOrder)
                : base(isMotorolaByteOrder)
            {
                if (baseOffset < 0)
                    throw new ArgumentOutOfRangeException("baseOffset", "Must be zero or greater.");

                _baseReader = baseReader;
                _baseOffset = baseOffset;
            }

            public override IndexedReader WithByteOrder(bool isMotorolaByteOrder) { return isMotorolaByteOrder == IsMotorolaByteOrder ? this : new ShiftedIndexedCapturingReader(_baseReader, _baseOffset, isMotorolaByteOrder); }

            public override IndexedReader WithShiftedBaseOffset(int shift) { return shift == 0 ? this : new ShiftedIndexedCapturingReader(_baseReader, _baseOffset + shift, IsMotorolaByteOrder); }

            public override int ToUnshiftedOffset(int localOffset) { return localOffset + _baseOffset; }

            public override byte GetByte(int index) { return _baseReader.GetByte(_baseOffset + index); }

            public override byte[] GetBytes(int index, int count) { return _baseReader.GetBytes(_baseOffset + index, count); }

            protected override void ValidateIndex(int index, int bytesRequested) { _baseReader.ValidateIndex(index + _baseOffset, bytesRequested); }

            protected override bool IsValidIndex(int index, int bytesRequested) { return _baseReader.IsValidIndex(index + _baseOffset, bytesRequested); }

            public override long Length { get { return _baseReader.Length - _baseOffset; } }
        }
    }
}

namespace MetadataExtractor.Util
{
    /// <summary>
    /// Utility methods for date and time values.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    internal static class DateUtil
    {
        [Pure]
        public static bool IsValidDate(int year, int month, int day)
            { return year >= 1 && year <= 9999 &&
               month >= 1 && month <= 12 &&
               day >= 1 && day <= DateTime.DaysInMonth(year, month); }

        [Pure]
        public static bool IsValidTime(int hours, int minutes, int seconds)
            { return hours >= 0 && hours < 24 &&
               minutes >= 0 && minutes < 60 &&
               seconds >= 0 && seconds < 60; }

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromUnixTime(long unixTime) { return _unixEpoch.AddSeconds(unixTime); }
    }

    /// <summary>Contains helper methods that perform photographic conversions.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public static class PhotographicConversions
    {
        private const double Ln2 = 0.69314718055994530941723212145818d;
        private const double RootTwo = 1.4142135623730950488016887242097d;

        /// <summary>Converts an aperture value to its corresponding F-stop number.</summary>
        /// <param name="aperture">the aperture value to convert</param>
        /// <returns>the F-stop number of the specified aperture</returns>
        public static double ApertureToFStop(double aperture) { return Math.Pow(RootTwo, aperture); }

        // NOTE jhead uses a different calculation as far as i can tell...  this confuses me...
        // fStop = (float)Math.exp(aperture * Math.log(2) * 0.5));
        /// <summary>Converts a shutter speed to an exposure time.</summary>
        /// <param name="shutterSpeed">the shutter speed to convert</param>
        /// <returns>the exposure time of the specified shutter speed</returns>
        public static double ShutterSpeedToExposureTime(double shutterSpeed) { return (float)(1 / Math.Exp(shutterSpeed * Ln2)); }
    }

    public static class ByteConvert
    {
        [Pure]
        public static uint FromBigEndianToNative(uint bigEndian)
        {
            if (BitConverter.IsLittleEndian == false)
                return bigEndian;

            var bytes = BitConverter.GetBytes(bigEndian);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, startIndex: 0);
        }

        [Pure]
        public static ushort FromBigEndianToNative(ushort bigEndian)
        {
            if (BitConverter.IsLittleEndian == false)
                return bigEndian;

            var bytes = BitConverter.GetBytes(bigEndian);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, startIndex: 0);
        }

        [Pure]
        public static short FromBigEndianToNative(short bigEndian)
        {
            if (BitConverter.IsLittleEndian == false)
                return bigEndian;

            var bytes = BitConverter.GetBytes(bigEndian);
            Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, startIndex: 0);
        }

        [Pure]
        public static int ToInt32BigEndian([NotNull] byte[] bytes) { return bytes[0] << 24 |
                                                                      bytes[1] << 16 |
                                                                      bytes[2] << 8 |
                                                                      bytes[3]; }

        [Pure]
        public static int ToInt32LittleEndian([NotNull] byte[] bytes) { return bytes[0] |
                                                                         bytes[1] << 8 |
                                                                         bytes[2] << 16 |
                                                                         bytes[3] << 24; }
    }

    /// <summary>Enumeration of supported image file formats.</summary>
    public enum FileType
    {
        /// <summary>File type is not known.</summary>
        Unknown = 0,

        /// <summary>Joint Photographic Experts Group (JPEG).</summary>
        Jpeg = 1,

        /// <summary>Tagged Image File Format (TIFF).</summary>
        Tiff = 2,

        /// <summary>Photoshop Document.</summary>
        Psd = 3,

        /// <summary>Portable Network Graphic (PNG).</summary>
        Png = 4,

        /// <summary>Bitmap (BMP).</summary>
        Bmp = 5,

        /// <summary>Graphics Interchange Format (GIF).</summary>
        Gif = 6,

        /// <summary>Windows Icon.</summary>
        Ico = 7,

        /// <summary>PiCture eXchange.</summary>
        Pcx = 8,

        /// <summary>Resource Interchange File Format.</summary>
        Riff = 9,
        
        /// <summary>Waveform Audio File Format.</summary>
        Wav = 10, // ("WAV", "Waveform Audio File Format", "audio/vnd.wave", "wav", "wave"),
        
        /// <summary>Audio Video Interleaved.</summary>
        Avi = 11, //("AVI", "Audio Video Interleaved", "video/vnd.avi", "avi"),
        
        /// <summary>WebP.</summary>
        WebP = 12, //("WebP", "WebP", "image/webp", "webp"),

        /// <summary>Sony camera raw.</summary>
        Arw = 13,

        /// <summary>Canon camera raw (version 1).</summary>
        Crw = 14,

        /// <summary>Canon camera raw (version 2).</summary>
        Cr2 = 15,

        /// <summary>Nikon camera raw.</summary>
        Nef = 16,

        /// <summary>Olympus camera raw.</summary>
        Orf = 17,

        /// <summary>Fujifilm camera raw.</summary>
        Raf = 18,

        /// <summary>Panasonic camera raw.</summary>
        Rw2 = 19,

        /// <summary>QuickTime (mov) format video.</summary>
        QuickTime = 20,

        /// <summary>Netpbm family of image formats.</summary>
        Netpbm = 21
    }

    public static class FileTypeExtensions
    {
        private static readonly string[] _shortNames =
        {
            "Unknown",
            "JPEG",
            "TIFF",
            "PSD",
            "PNG",
            "BMP",
            "GIF",
            "ICO",
            "PCX",
            "RIFF",
            "WAV",
            "AVI",
            "WebP",
            "ARW",
            "CRW",
            "CR2",
            "NEF",
            "ORF",
            "RAF",
            "RW2",
            "QuickTime",
            "Netpbm"
        };
        
        private static readonly string[] _longNames =
        {
            "Unknown",
            "Joint Photographic Experts Group",
            "Tagged Image File Format",
            "Photoshop Document",
            "Portable Network Graphics",
            "Device Independent Bitmap",
            "Graphics Interchange Format",
            "Windows Icon",
            "PiCture eXchange",
            "Resource Interchange File Format",
            "Waveform Audio File Format",
            "Audio Video Interleaved",
            "WebP",
            "Sony Camera Raw",
            "Canon Camera Raw",
            "Canon Camera Raw",
            "Nikon Camera Raw",
            "Olympus Camera Raw",
            "FujiFilm Camera Raw",
            "Panasonic Camera Raw",
            "QuickTime",
            "Netpbm"
        };

        [ItemCanBeNull] private static readonly string[] _mimeTypes =
        {
            null,
            "image/jpeg",
            "image/tiff",
            "image/vnd.adobe.photoshop",
            "image/png",
            "image/bmp",
            "image/gif",
            "image/x-icon",
            "image/x-pcx",
            null, // RIFF
            "audio/vnd.wave",
            "video/vnd.avi",
            "image/webp",
            null, // Sony RAW
            null,
            null,
            null,
            null,
            null,
            null,
            "video/quicktime",
            "image/x-portable-graymap"
        };

        [ItemCanBeNull] private static readonly string[][] _extensions =
        {
            null,
            new[] { "jpg", "jpeg", "jpe" },
            new[] { "tiff", "tif" },
            new[] { "psd" },
            new[] { "png" },
            new[] { "bmp" },
            new[] { "gif" },
            new[] { "ico" },
            new[] { "pcx" },
            null, // RIFF
            new[] { "wav", "wave" },
            new[] { "avi" },
            new[] { "webp" },
            new[] { "arw" },
            new[] { "crw" },
            new[] { "cr2" },
            new[] { "nef" },
            new[] { "orf" },
            new[] { "raf" },
            new[] { "rw2" },
            new[] { "mov" },
            new[] { "pbm", "ppm" }
        };
        
        [NotNull]
        public static string GetName(this FileType fileType)
        {
            var i = (int)fileType;
            if (i < 0 || i >= _shortNames.Length)
                throw new ArgumentException("Invalid " + "FileType" + " enum member.", "fileType");
            return _shortNames[i];
        }
        
        [NotNull]
        public static string GetLongName(this FileType fileType)
        {
            var i = (int)fileType;
            if (i < 0 || i >= _longNames.Length)
                throw new ArgumentException("Invalid " + "FileType" + " enum member.", "fileType");
            return _longNames[i];
        }
        
        [CanBeNull]
        public static string GetMimeType(this FileType fileType)
        {
            var i = (int)fileType;
            if (i < 0 || i >= _mimeTypes.Length)
                throw new ArgumentException("Invalid " + "FileType" + " enum member.", "fileType");
            return _mimeTypes[i];
        }
        
        [CanBeNull]
        public static string GetCommonExtension(this FileType fileType)
        {
            var i = (int)fileType;
            if (i < 0 || i >= _extensions.Length)
                throw new ArgumentException("Invalid " + "FileType" + " enum member.", "fileType");
            return (_extensions[i] != null && _extensions[i].Length > 0 ? _extensions[i][0] : null);
        }
        
        [CanBeNull]
        public static IEnumerable<string> GetAllExtensions(this FileType fileType)
        {
            var i = (int)fileType;
            if (i < 0 || i >= _mimeTypes.Length)
                throw new ArgumentException("Invalid " + "FileType" + " enum member.", "fileType");
            return _extensions[i];
        }
    }

    /// <summary>Stores values using a prefix tree (aka 'trie', i.e. reTRIEval data structure).</summary>
    public sealed class ByteTrie<T> : IEnumerable<T>
    {
        /// <summary>A node in the trie.</summary>
        /// <remarks>Has children and may have an associated value.</remarks>
        private sealed class ByteTrieNode
        {
            public readonly IDictionary<byte, ByteTrieNode> Children = new Dictionary<byte, ByteTrieNode>();

            public T Value { get; private set; }
            public bool HasValue { get; private set; }

            public void SetValue(T value)
            {
                Debug.Assert(!HasValue, "Value already set for this trie node");
                Value = value;
                HasValue = true;
            }
        }

        private readonly ByteTrieNode _root = new ByteTrieNode();

        /// <summary>Gets the maximum depth stored in this trie.</summary>
        public int MaxDepth { get; private set; }

        public ByteTrie() {}

        public ByteTrie(T defaultValue) { SetDefaultValue(defaultValue); }

        /// <summary>Return the most specific value stored for this byte sequence.</summary>
        /// <remarks>
        /// If not found, returns <c>null</c> or a default values as specified by
        /// calling <see cref="SetDefaultValue"/>.
        /// </remarks>
        [CanBeNull]
        [SuppressMessage("ReSharper", "ParameterTypeCanBeEnumerable.Global")]
        public T Find([NotNull] byte[] bytes)
        {
            var node = _root;
            var value = node.Value;
            foreach (var b in bytes)
            {
                if (!node.Children.TryGetValue(b, out node))
                    break;
                if (node.HasValue)
                    value = node.Value;
            }
            return value;
        }

        /// <summary>Store the given value at the specified path.</summary>
        public void Add(T value, [NotNull] params byte[][] parts)
        {
            var depth = 0;
            var node = _root;
            foreach (var part in parts)
            {
                foreach (var b in part)
                {
                    ByteTrieNode child;
                    if (!node.Children.TryGetValue(b, out child))
                    {
                        child = new ByteTrieNode();
                        node.Children[b] = child;
                    }
                    node = child;
                    depth++;
                }
            }
            node.SetValue(value);
            MaxDepth = Math.Max(MaxDepth, depth);
        }

        /// <summary>
        /// Sets the default value to use in <see cref="ByteTrie{T}.Find(byte[])"/> when no path matches.
        /// </summary>
        public void SetDefaultValue(T defaultValue) { _root.SetValue(defaultValue); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { throw new NotImplementedException(); }
    }

    /// <summary>Examines the a file's first bytes and estimates the file's type.</summary>
    public static class FileTypeDetector
    {
        // https://en.wikipedia.org/wiki/List_of_file_signatures
        private static readonly ByteTrie<FileType> _root = new ByteTrie<FileType>(defaultValue: FileType.Unknown)
        {
            { FileType.Jpeg, new[] { (byte)0xff, (byte)0xd8 } },
            { FileType.Tiff, Encoding.UTF8.GetBytes("II"), new byte[] { 0x2a, 0x00 } },
            { FileType.Tiff, Encoding.UTF8.GetBytes("MM"), new byte[] { 0x00, 0x2a } },
            { FileType.Psd, Encoding.UTF8.GetBytes("8BPS") },
            { FileType.Png, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 } },
            { FileType.Bmp, Encoding.UTF8.GetBytes("BM") },
            // TODO technically there are other very rare magic numbers for OS/2 BMP files
            { FileType.Gif, Encoding.UTF8.GetBytes("GIF87a") },
            { FileType.Gif, Encoding.UTF8.GetBytes("GIF89a") },
            { FileType.Ico, new byte[] { 0x00, 0x00, 0x01, 0x00 } },
            { FileType.Netpbm, Encoding.UTF8.GetBytes("P1") }, // ASCII B
            { FileType.Netpbm, Encoding.UTF8.GetBytes("P2") }, // ASCII greysca
            { FileType.Netpbm, Encoding.UTF8.GetBytes("P3") }, // ASCII R
            { FileType.Netpbm, Encoding.UTF8.GetBytes("P4") }, // RAW B
            { FileType.Netpbm, Encoding.UTF8.GetBytes("P5") }, // RAW greysca
            { FileType.Netpbm, Encoding.UTF8.GetBytes("P6") }, // RAW R
            { FileType.Netpbm, Encoding.UTF8.GetBytes("P7") }, // P
            { FileType.Pcx, new byte[] { 0x0A, 0x00, 0x01 } },
            // multiple PCX versions, explicitly list
            { FileType.Pcx, new byte[] { 0x0A, 0x02, 0x01 } },
            { FileType.Pcx, new byte[] { 0x0A, 0x03, 0x01 } },
            { FileType.Pcx, new byte[] { 0x0A, 0x05, 0x01 } },
            { FileType.Riff, Encoding.UTF8.GetBytes("RIFF") },
            { FileType.Arw, Encoding.UTF8.GetBytes("II"), new byte[] { 0x2a, 0x00, 0x08, 0x00 } },
            { FileType.Crw, Encoding.UTF8.GetBytes("II"), new byte[] { 0x1a, 0x00, 0x00, 0x00 }, Encoding.UTF8.GetBytes("HEAPCCDR") },
            { FileType.Cr2, Encoding.UTF8.GetBytes("II"), new byte[] { 0x2a, 0x00, 0x10, 0x00, 0x00, 0x00, 0x43, 0x52 } },
            // NOTE this doesn't work for NEF as it incorrectly flags many other TIFF files as being NEF
//            { FileType.Nef, Encoding.UTF8.GetBytes("MM"), new byte[] { 0x00, 0x2a, 0x00, 0x00, 0x00, 0x08, 0x00 } },
            { FileType.Orf, Encoding.UTF8.GetBytes("IIRO"), new byte[] { 0x08, 0x00 } },
            { FileType.Orf, Encoding.UTF8.GetBytes("MMOR"), new byte[] { 0x00, 0x00 } },
            { FileType.Orf, Encoding.UTF8.GetBytes("IIRS"), new byte[] { 0x08, 0x00 } },
            { FileType.Raf, Encoding.UTF8.GetBytes("FUJIFILMCCD-RAW") },
            { FileType.Rw2, Encoding.UTF8.GetBytes("II"), new byte[] { 0x55, 0x00 } },
        };

        private static readonly IEnumerable<Func<byte[], FileType>> _fixedCheckers = new Func<byte[], FileType>[]
        {
            bytes => bytes.RegionEquals(4, 4, Encoding.UTF8.GetBytes("ftyp"))
                ? FileType.QuickTime
                : FileType.Unknown
        };

        /// <summary>Examines the a file's first bytes and estimates the file's type.</summary>
        /// <exception cref="ArgumentException">Stream does not support seeking.</exception>
        /// <exception cref="IOException">An IO error occurred, or the input stream ended unexpectedly.</exception>
        public static FileType DetectFileType([NotNull] Stream stream)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("Must support seek", "stream");

            var maxByteCount = _root.MaxDepth;

            var bytes = new byte[maxByteCount];
            var bytesRead = stream.Read(bytes, 0, bytes.Length);

            if (bytesRead == 0)
                return FileType.Unknown;

            stream.Seek(-bytesRead, SeekOrigin.Current);

            var fileType = _root.Find(bytes);

            if (fileType == FileType.Unknown)
            {
                foreach (var fixedChecker in _fixedCheckers)
                {
                    fileType = fixedChecker(bytes);
                    if (fileType != FileType.Unknown)
                        return fileType;
                }
            }
            else if (fileType == FileType.Riff)
            {
                var fourCC = Encoding.UTF8.GetString(bytes, index: 8, count: 4);
                switch (fourCC)
                {
                    case "WAVE":
                        return FileType.Wav;
                    case "AVI ":
                        return FileType.Avi;
                    case "WEBP":
                        return FileType.WebP;
                }
            }

            return fileType;
        }
    }

    internal static class ByteArrayExtensions
    {
        public static bool RegionEquals([NotNull] this byte[] bytes, int offset, int count, [NotNull] byte[] comparand)
        {
            if (offset < 0 ||                   // invalid arg
                count < 0 ||                    // invalid arg
                bytes.Length < offset + count)  // extends beyond end
                return false;

            for (int i = 0, j = offset; i < count; i++, j++)
            {
                if (bytes[j] != comparand[i])
                    return false;
            }

            return true;
        }
    }
}

namespace MetadataExtractor.Formats.FileType
{
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class FileTypeDescriptor : TagDescriptor<FileTypeDirectory>
    {
        public FileTypeDescriptor([NotNull] FileTypeDirectory directory)
            : base(directory)
        {
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class FileTypeDirectory : Directory
    {
        public const int TagDetectedFileTypeName = 1;
        public const int TagDetectedFileTypeLongName = 2;
        public const int TagDetectedFileMimeType = 3;
        public const int TagExpectedFileNameExtension = 4;

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagDetectedFileTypeName, "Detected File Type Name" },
            { TagDetectedFileTypeLongName, "Detected File Type Long Name" },
            { TagDetectedFileMimeType, "Detected MIME Type" },
            { TagExpectedFileNameExtension, "Expected File Name Extension" },
        };

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public FileTypeDirectory(Util.FileType fileType)
        {
            SetDescriptor(new FileTypeDescriptor(this));

            var name = fileType.GetName();
                        
            Set(TagDetectedFileTypeName, name);
            Set(TagDetectedFileTypeLongName, fileType.GetLongName());

            var mimeType = fileType.GetMimeType();
            if (mimeType != null)
                Set(TagDetectedFileMimeType, mimeType);

            var extension = fileType.GetCommonExtension();
            if (extension != null)
                Set(TagExpectedFileNameExtension, extension);
        }

        public override string Name { get { return "File Type"; } }

        protected override bool TryGetTagName(int tagType, out string tagName) { return _tagNameMap.TryGetValue(tagType, out tagName); }
    }
}

namespace MetadataExtractor.Formats.QuickTime
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class QuickTimeTrackHeaderDirectory : Directory
    {
        public const int TagVersion = 1;
        public const int TagFlags = 2;
        public const int TagCreated = 3;
        public const int TagModified = 4;
        public const int TagTrackId = 5;
        public const int TagDuration = 6;
        public const int TagLayer = 7;
        public const int TagAlternateGroup = 8;
        public const int TagVolume = 9;
        public const int TagWidth = 10;
        public const int TagHeight = 11;

        public override string Name { get { return "QuickTime Track Header"; } }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagVersion,        "Version" },
            { TagFlags,          "Flags" },
            { TagCreated,        "Created" },
            { TagModified,       "Modified" },
            { TagTrackId,        "TrackId" },
            { TagDuration,       "Duration" },
            { TagLayer,          "Layer" },
            { TagAlternateGroup, "Alternate Group" },
            { TagVolume,         "Volume" },
            { TagWidth,          "Width" },
            { TagHeight,         "Height" }
        };

        public QuickTimeTrackHeaderDirectory()
        {
            SetDescriptor(new TagDescriptor<QuickTimeTrackHeaderDirectory>(this));
        }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    /// <summary>
    /// Models data provided to callbacks invoked when reading QuickTime atoms via <see cref="QuickTimeReader.ProcessAtoms"/>.
    /// </summary>
    public sealed class AtomCallbackArgs
    {
        /// <summary>
        /// Gets the 32-bit unsigned integer that identifies the atom's type.
        /// </summary>
        public uint Type { get; private set; }

        /// <summary>
        /// The length of the atom data, in bytes. If the atom extends to the end of the file, this value is zero.
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        /// Gets the stream from which atoms are being read.
        /// </summary>
        public Stream Stream { get; private set; }

        /// <summary>
        /// Gets the position within <see cref="Stream"/> at which this atom's data started.
        /// </summary>
        public long StartPosition { get; private set; }

        /// <summary>
        /// Gets a sequential reader from which this atom's contents may be read.
        /// </summary>
        /// <remarks>
        /// It is backed by <see cref="Stream"/>, so manipulating the stream's position will influence this reader.
        /// </remarks>
        public SequentialStreamReader Reader { get; private set; }

        /// <summary>
        /// Gets and sets whether the callback wishes processing to terminate.
        /// </summary>
        public bool Cancel { get; set; }

        public AtomCallbackArgs(uint type, long size, Stream stream, long startPosition, SequentialStreamReader reader)
        {
            Type = type;
            Size = size;
            Stream = stream;
            StartPosition = startPosition;
            Reader = reader;
        }

        /// <summary>
        /// Gets the string representation of this atom's type.
        /// </summary>
        [NotNull]
        public string TypeString
        {
            get
            {
                var bytes = BitConverter.GetBytes(Type);
                Array.Reverse(bytes);
#if NETSTANDARD1_3
                return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
#else
                return Encoding.ASCII.GetString(bytes);
#endif
            }
        }

        /// <summary>
        /// Computes the number of bytes remaining in the atom, given the <see cref="Stream"/> position.
        /// </summary>
        public long BytesLeft { get { return Size - (Stream.Position - StartPosition); } }
    }

    /// <summary>
    /// Static class for processing atoms the QuickTime container format.
    /// </summary>
    /// <remarks>
    /// QuickTime file format specification: https://developer.apple.com/library/mac/documentation/QuickTime/QTFF/qtff.pdf
    /// </remarks>
    public static class QuickTimeReader
    {
        /// <summary>
        /// Reads atom data from <paramref name="stream"/>, invoking <paramref name="handler"/> for each atom encountered.
        /// </summary>
        /// <param name="stream">The stream to read atoms from.</param>
        /// <param name="handler">A callback function to handle each atom.</param>
        /// <param name="stopByBytes">The maximum number of bytes to process before discontinuing.</param>
        public static void ProcessAtoms([NotNull] Stream stream, [NotNull] Action<AtomCallbackArgs> handler, long stopByBytes = -1)
        {
            var reader = new SequentialStreamReader(stream);

            var seriesStartPos = stream.Position;

            while (stopByBytes == -1 || stream.Position < seriesStartPos + stopByBytes)
            {
                var atomStartPos = stream.Position;

                try
                {
                    // Check if the end of the stream is closer then 8 bytes to current position (Length of the atom's data + atom type)
                    if (reader.IsCloserToEnd(8))
                        return;

                    // Length of the atom's data, in bytes, including size bytes
                    long atomSize = reader.GetUInt32();

                    // Typically four ASCII characters, but may be non-printable.
                    // By convention, lowercase 4CCs are reserved by Apple.
                    var atomType = reader.GetUInt32();

                    if (atomSize == 1)
                    {
                        // Check if the end of the stream is closer then 8 bytes
                        if (reader.IsCloserToEnd(8))
                            return;

                        // Size doesn't fit in 32 bits so read the 64 bit size here
                        atomSize = checked((long)reader.GetUInt64());
                    }
                    else if (atomSize < 8)
                    {
                        // Atom should be at least 8 bytes long
                        return;
                    }

                    var args = new AtomCallbackArgs(atomType, atomSize, stream, atomStartPos, reader);

                    handler(args);

                    if (args.Cancel)
                        return;

                    if (atomSize == 0)
                        return;

                    var toSkip = atomStartPos + atomSize - stream.Position;

                    if (toSkip < 0)
                        throw new Exception("Handler moved stream beyond end of atom");

                    // To avoid exception handling we can check if needed number of bytes are available
                    if (!reader.IsCloserToEnd(toSkip))
                        reader.TrySkip(toSkip);
                }
                catch (IOException)
                {
                    // Exception trapping is used when stream doesn't support stream length method only
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for reading QuickTime specific encodings from a <see cref="SequentialReader"/>.
    /// </summary>
    public static class QuickTimeReaderExtensions
    {
        [NotNull]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static string Get4ccString([NotNull] this SequentialReader reader)
        {
            var sb = new StringBuilder(4);
            sb.Append((char) reader.GetByte());
            sb.Append((char) reader.GetByte());
            sb.Append((char) reader.GetByte());
            sb.Append((char) reader.GetByte());
            return sb.ToString();
        }

        public static decimal Get16BitFixedPoint([NotNull] this SequentialReader reader)
        {
            return decimal.Add(
                reader.GetByte(),
                decimal.Divide(reader.GetByte(), byte.MaxValue));
        }

        public static decimal Get32BitFixedPoint([NotNull] this SequentialReader reader)
        {
            return decimal.Add(
                reader.GetUInt16(),
                decimal.Divide(reader.GetUInt16(), ushort.MaxValue));
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class QuickTimeMovieHeaderDirectory : Directory
    {
        public const int TagVersion = 1;
        public const int TagFlags = 2;
        public const int TagCreated = 3;
        public const int TagModified = 4;
        public const int TagTimeScale = 5;
        public const int TagDuration = 6;
        public const int TagPreferredRate = 7;
        public const int TagPreferredVolume = 8;
        public const int TagMatrix = 9;
        public const int TagPreviewTime = 10;
        public const int TagPreviewDuration = 11;
        public const int TagPosterTime = 12;
        public const int TagSelectionTime = 13;
        public const int TagSelectionDuration = 14;
        public const int TagCurrentTime = 15;
        public const int TagNextTrackId = 16;

        public override string Name { get { return "QuickTime Movie Header"; } }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagVersion,           "Version" },
            { TagFlags,             "Flags" },
            { TagCreated,           "Created" },
            { TagModified,          "Modified" },
            { TagTimeScale,         "TrackId" },
            { TagDuration,          "Duration" },
            { TagPreferredRate,     "Preferred Rate" },
            { TagPreferredVolume,   "Preferred Volume" },
            { TagMatrix,            "Matrix" },
            { TagPreviewTime,       "Preview Time" },
            { TagPreviewDuration,   "Preview Duration" },
            { TagPosterTime,        "Poster Time" },
            { TagSelectionTime,     "Selection Time" },
            { TagSelectionDuration, "Selection Duration" },
            { TagCurrentTime,       "Current Time" },
            { TagNextTrackId,       "Next Track Id" }
        };

        public QuickTimeMovieHeaderDirectory()
        {
            SetDescriptor(new TagDescriptor<QuickTimeMovieHeaderDirectory>(this));
        }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class QuickTimeFileTypeDirectory : Directory
    {
        public const int TagMajorBrand = 1;
        public const int TagMinorVersion = 2;
        public const int TagCompatibleBrands = 3;

        public override string Name { get { return "QuickTime File Type"; } }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagMajorBrand,       "Major Brand" },
            { TagMinorVersion,     "Minor Version" },
            { TagCompatibleBrands, "Compatible Brands" }
        };

        public QuickTimeFileTypeDirectory()
        {
            SetDescriptor(new TagDescriptor<QuickTimeFileTypeDirectory>(this));
        }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }
    
    public sealed class QuickTimeMetaDirectory : Directory
    {
        public const int TagMake = 1;
        public const int TagModel = 2;
        public const int TagSoftware = 3;
        public const int TagCreationDate = 4;
        public const int TagCreationDateNoTimeZone = 5;

        public override string Name { get { return "QuickTime Meta Data"; } }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagMake, "Make" },
            { TagModel, "Model" },
            { TagSoftware, "Software" },
            { TagCreationDate, "Creation Date" },
            { TagCreationDateNoTimeZone, "Creation Date (without time zone)" }
        };

        public QuickTimeMetaDirectory()
        {
            SetDescriptor(new TagDescriptor<QuickTimeMetaDirectory>(this));
        }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    public static class QuickTimeMetadataReader
    {
        private static readonly DateTime _epoch = new DateTime(1904, 1, 1);

        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] Stream stream)
        {
            List<Directory> directories = new List<Directory>();

            Action<AtomCallbackArgs> TrakHandler = (AtomCallbackArgs a) =>
            {
                switch (a.TypeString)
                {
                    case "tkhd":
                    {
                        var directory = new QuickTimeTrackHeaderDirectory();
                        directory.Set(QuickTimeTrackHeaderDirectory.TagVersion, a.Reader.GetByte());
                        directory.Set(QuickTimeTrackHeaderDirectory.TagFlags, a.Reader.GetBytes(3));
                        directory.Set(QuickTimeTrackHeaderDirectory.TagCreated, _epoch.AddTicks(TimeSpan.TicksPerSecond*a.Reader.GetUInt32()));
                        directory.Set(QuickTimeTrackHeaderDirectory.TagModified, _epoch.AddTicks(TimeSpan.TicksPerSecond*a.Reader.GetUInt32()));
                        directory.Set(QuickTimeTrackHeaderDirectory.TagTrackId, a.Reader.GetUInt32());
                        a.Reader.Skip(4L);
                        directory.Set(QuickTimeTrackHeaderDirectory.TagDuration, a.Reader.GetUInt32());
                        a.Reader.Skip(8L);
                        directory.Set(QuickTimeTrackHeaderDirectory.TagLayer, a.Reader.GetUInt16());
                        directory.Set(QuickTimeTrackHeaderDirectory.TagAlternateGroup, a.Reader.GetUInt16());
                        directory.Set(QuickTimeTrackHeaderDirectory.TagVolume, a.Reader.Get16BitFixedPoint());
                        a.Reader.Skip(2L);
                        a.Reader.GetBytes(36);
                        directory.Set(QuickTimeTrackHeaderDirectory.TagWidth, a.Reader.Get32BitFixedPoint());
                        directory.Set(QuickTimeTrackHeaderDirectory.TagHeight, a.Reader.Get32BitFixedPoint());
                        directories.Add(directory);
                        break;
                    }
                }
            };

            Action<AtomCallbackArgs> MoovHandler = (AtomCallbackArgs a) =>
            {
                switch (a.TypeString)
                {
                    case "mvhd":
                    {
                        var directory = new QuickTimeMovieHeaderDirectory();
                        directory.Set(QuickTimeMovieHeaderDirectory.TagVersion, a.Reader.GetByte());
                        directory.Set(QuickTimeMovieHeaderDirectory.TagFlags, a.Reader.GetBytes(3));
                        directory.Set(QuickTimeMovieHeaderDirectory.TagCreated, _epoch.AddTicks(TimeSpan.TicksPerSecond*a.Reader.GetUInt32()));
                        directory.Set(QuickTimeMovieHeaderDirectory.TagModified, _epoch.AddTicks(TimeSpan.TicksPerSecond*a.Reader.GetUInt32()));
                        var timeScale = a.Reader.GetUInt32();
                        directory.Set(QuickTimeMovieHeaderDirectory.TagTimeScale, timeScale);
                        directory.Set(QuickTimeMovieHeaderDirectory.TagDuration, TimeSpan.FromSeconds(a.Reader.GetUInt32()/(double) timeScale));
                        directory.Set(QuickTimeMovieHeaderDirectory.TagPreferredRate, a.Reader.Get32BitFixedPoint());
                        directory.Set(QuickTimeMovieHeaderDirectory.TagPreferredVolume, a.Reader.Get16BitFixedPoint());
                        a.Reader.Skip(10);
                        directory.Set(QuickTimeMovieHeaderDirectory.TagMatrix, a.Reader.GetBytes(36));
                        directory.Set(QuickTimeMovieHeaderDirectory.TagPreviewTime, a.Reader.GetUInt32());
                        directory.Set(QuickTimeMovieHeaderDirectory.TagPreviewDuration, a.Reader.GetUInt32());
                        directory.Set(QuickTimeMovieHeaderDirectory.TagPosterTime, a.Reader.GetUInt32());
                        directory.Set(QuickTimeMovieHeaderDirectory.TagSelectionTime, a.Reader.GetUInt32());
                        directory.Set(QuickTimeMovieHeaderDirectory.TagSelectionDuration, a.Reader.GetUInt32());
                        directory.Set(QuickTimeMovieHeaderDirectory.TagCurrentTime, a.Reader.GetUInt32());
                        directory.Set(QuickTimeMovieHeaderDirectory.TagNextTrackId, a.Reader.GetUInt32());
                        directories.Add(directory);
                        break;
                    }
                    case "trak":
                    {
                        QuickTimeReader.ProcessAtoms(stream, TrakHandler, a.BytesLeft);
                        break;
                    }
//                    case "clip":
//                    {
//                        QuickTimeReader.ProcessAtoms(stream, clipHandler, a.BytesLeft);
//                        break;
//                    }
//                    case "prfl":
//                    {
//                        a.Reader.Skip(4L);
//                        var partId = a.Reader.GetUInt32();
//                        var featureCode = a.Reader.GetUInt32();
//                        var featureValue = string.Join(" ", a.Reader.GetBytes(4).Select(v => v.ToString("X2")).ToArray());
//                        Debug.WriteLine($"PartId={partId} FeatureCode={featureCode} FeatureValue={featureValue}");
//                        break;
//                    }
                    case "meta":
                    {
                        List<string> keys = new List<string>(), vals = new List<string>();
                        QuickTimeReader.ProcessAtoms(stream, (AtomCallbackArgs ameta) =>
                        {
                            switch (ameta.TypeString)
                            {
                                case "keys":
                                    if (ameta.BytesLeft <= 16) break;
                                    ushort keys_version = ameta.Reader.GetUInt16(), keys_flags = ameta.Reader.GetUInt16();
                                    uint keys_count = ameta.Reader.GetUInt32();
                                    for (uint k = 0; ameta.BytesLeft > 8 && k < keys_count; k++)
                                    {
                                        int key_size = ameta.Reader.GetInt32();
                                        string key_namespace = ameta.Reader.Get4ccString();
                                        string key_name = ameta.Reader.GetString(key_size - 8, Encoding.UTF8);
                                        keys.Add(key_name);
                                    }
                                    break;
                                case "ilst":
                                    QuickTimeReader.ProcessAtoms(stream, (AtomCallbackArgs ailst) => { QuickTimeReader.ProcessAtoms(stream, (AtomCallbackArgs adata) =>
                                    {
                                        if (adata.BytesLeft <= 8) return;
                                        adata.Reader.Skip(8);
                                        vals.Add(adata.Reader.GetString((int)adata.BytesLeft, Encoding.UTF8));
                                    }, ailst.BytesLeft); }, ameta.BytesLeft);
                                    break;
                            }
                        }, a.BytesLeft);
                        var directory = new QuickTimeMetaDirectory();
                        for (int i = 0, iMax = Math.Min(keys.Count, vals.Count); i != iMax; i++)
                        {
                            if (keys[i] == "com.apple.quicktime.make") directory.Set(QuickTimeMetaDirectory.TagMake, vals[i]);
                            if (keys[i] == "com.apple.quicktime.model") directory.Set(QuickTimeMetaDirectory.TagModel, vals[i]);
                            if (keys[i] == "com.apple.quicktime.software") directory.Set(QuickTimeMetaDirectory.TagSoftware, vals[i]);
                            if (keys[i] == "com.apple.quicktime.creationdate")
                            {
                                DateTime dt;
                                int plus = vals[i].IndexOf('+');
                                if (plus > 0 && DateTime.TryParse(vals[i].Substring(0, plus), out dt)) directory.Set(QuickTimeMetaDirectory.TagCreationDateNoTimeZone, dt);
                                if (DateTime.TryParse(vals[i], out dt)) directory.Set(QuickTimeMetaDirectory.TagCreationDate, dt);
                                else directory.Set(QuickTimeMetaDirectory.TagCreationDate, vals[i]);
                            }
                        }
                        directories.Add(directory);
                        break;
                    }
                }
            };

            Action<AtomCallbackArgs> Handler = (AtomCallbackArgs a) =>
            {
                switch (a.TypeString)
                {
                    case "moov":
                    {
                        QuickTimeReader.ProcessAtoms(stream, MoovHandler, a.BytesLeft);
                        break;
                    }
                    case "ftyp":
                    {
                        var directory = new QuickTimeFileTypeDirectory();
                        directory.Set(QuickTimeFileTypeDirectory.TagMajorBrand, a.Reader.Get4ccString());
                        directory.Set(QuickTimeFileTypeDirectory.TagMinorVersion, a.Reader.GetUInt32());
                        var compatibleBrands = new List<string>();
                        while (a.BytesLeft >= 4)
                            compatibleBrands.Add(a.Reader.Get4ccString());
                        directory.Set(QuickTimeFileTypeDirectory.TagCompatibleBrands, compatibleBrands);
                        directories.Add(directory);
                        break;
                    }
                }
            };

            QuickTimeReader.ProcessAtoms(stream, Handler);

            return directories;
        }
    }
}


namespace MetadataExtractor.Formats.Tiff
{
    using MetadataExtractor.Formats.Exif;

    /// <summary>Obtains all available metadata from TIFF formatted files.</summary>
    /// <remarks>
    /// Obtains all available metadata from TIFF formatted files.  Note that TIFF files include many digital camera RAW
    /// formats, including Canon (CRW, CR2), Nikon (NEF), Olympus (ORF) and Panasonic (RW2).
    /// </remarks>
    /// <author>Darren Salomons</author>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public static class TiffMetadataReader
    {
        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="TiffProcessingException"/>
        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] string filePath)
        {
            var directories = new List<Directory>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess))
            {
                var handler = new ExifTiffHandler(directories);
                TiffReader.ProcessTiff(new IndexedSeekingReader(stream), handler);
            }

#if METADATAEXTRACTOR_HAVE_FILEMETADATA
            directories.Add(new FileMetadataReader().Read(filePath));
#endif

            return directories;
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="TiffProcessingException"/>
        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] Stream stream)
        {
            // TIFF processing requires random access, as directories can be scattered throughout the byte sequence.
            // Stream does not support seeking backwards, so we wrap it with IndexedCapturingReader, which
            // buffers data from the stream as we seek forward.
            var directories = new List<Directory>();

            var handler = new ExifTiffHandler(directories);
            TiffReader.ProcessTiff(new IndexedCapturingReader(stream), handler);

            return directories;
        }
    }
}

namespace MetadataExtractor.Formats.Tiff
{
    /// <summary>An exception class thrown upon unexpected and fatal conditions while processing a TIFF file.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    /// <author>Darren Salomons</author>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class TiffProcessingException : ImageProcessingException
    {
        public TiffProcessingException([CanBeNull] string message)
            : base(message)
        {
        }

        public TiffProcessingException([CanBeNull] string message, [CanBeNull] Exception innerException)
            : base(message, innerException)
        {
        }

        public TiffProcessingException([CanBeNull] Exception innerException)
            : base(innerException)
        {
        }

#if !NETSTANDARD1_3
        protected TiffProcessingException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>
    /// Interface of an class capable of handling events raised during the reading of a TIFF file
    /// via <see cref="TiffReader"/>.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public interface ITiffHandler
    {
        /// <summary>Receives the 2-byte marker found in the TIFF header.</summary>
        /// <remarks>
        /// Receives the 2-byte marker found in the TIFF header.
        /// <para />
        /// Implementations are not obligated to use this information for any purpose, though it may be useful for
        /// validation or perhaps differentiating the type of mapping to use for observed tags and IFDs.
        /// </remarks>
        /// <param name="marker">the 2-byte value found at position 2 of the TIFF header</param>
        /// <exception cref="TiffProcessingException"/>
        void SetTiffMarker(int marker);

        bool TryEnterSubIfd(int tagType);

        bool HasFollowerIfd();

        void EndingIfd();

        /// <exception cref="System.IO.IOException"/>
        bool CustomProcessTag(int tagOffset, [NotNull] ICollection<int> processedIfdOffsets, [NotNull] IndexedReader reader, int tagId, int byteCount);

        bool TryCustomProcessFormat(int tagId, TiffDataFormatCode formatCode, uint componentCount, out long byteCount);

        void Warn([NotNull] string message);

        void Error([NotNull] string message);

        void SetByteArray(int tagId, [NotNull] byte[] bytes);

        void SetString(int tagId, StringValue str);

        void SetRational(int tagId, Rational rational);

        void SetRationalArray(int tagId, [NotNull] Rational[] array);

        void SetFloat(int tagId, float float32);

        void SetFloatArray(int tagId, [NotNull] float[] array);

        void SetDouble(int tagId, double double64);

        void SetDoubleArray(int tagId, [NotNull] double[] array);

        void SetInt8S(int tagId, sbyte int8S);

        void SetInt8SArray(int tagId, [NotNull] sbyte[] array);

        void SetInt8U(int tagId, byte int8U);

        void SetInt8UArray(int tagId, [NotNull] byte[] array);

        void SetInt16S(int tagId, short int16S);

        void SetInt16SArray(int tagId, [NotNull] short[] array);

        void SetInt16U(int tagId, ushort int16U);

        void SetInt16UArray(int tagId, [NotNull] ushort[] array);

        void SetInt32S(int tagId, int int32S);

        void SetInt32SArray(int tagId, [NotNull] int[] array);

        void SetInt32U(int tagId, uint int32U);

        void SetInt32UArray(int tagId, [NotNull] uint[] array);
    }

    /// <summary>
    /// An implementation of <see cref="ITiffHandler"/> that stores tag values in <see cref="Directory"/> objects.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public abstract class DirectoryTiffHandler : ITiffHandler
    {
        private readonly Stack<Directory> _directoryStack = new Stack<Directory>();

        protected List<Directory> Directories { get { return _Directories; } }
        List<Directory> _Directories;

        [CanBeNull]
        protected Directory CurrentDirectory { get; private set; }

        protected DirectoryTiffHandler([NotNull] List<Directory> directories)
        {
            _Directories = directories;
        }

        public void EndingIfd()
        {
            CurrentDirectory = _directoryStack.Count == 0 ? null : _directoryStack.Pop();
        }

        protected void PushDirectory([NotNull] Directory directory)
        {
            // If this is the first directory, don't add to the stack
            if (CurrentDirectory != null)
            {
                _directoryStack.Push(CurrentDirectory);
                directory.Parent = CurrentDirectory;
            }
            CurrentDirectory = directory;
            Directories.Add(CurrentDirectory);
        }

        public void Warn(string message)  { GetCurrentOrErrorDirectory().AddError(message); }
        public void Error(string message) { GetCurrentOrErrorDirectory().AddError(message); }

        [NotNull]
        private Directory GetCurrentOrErrorDirectory()
        {
            if (CurrentDirectory != null)
                return CurrentDirectory;
            Directory error = Directories.Find((Directory a) => { return a is ErrorDirectory; });
            if (error != null)
                return error;
            error = new ErrorDirectory();
            PushDirectory(error);
            return error;
        }

        public void SetByteArray(int tagId, byte[] bytes)         { CurrentDirectory.Set(tagId, bytes);       }
        public void SetString(int tagId, StringValue stringValue) { CurrentDirectory.Set(tagId, stringValue); }
        public void SetRational(int tagId, Rational rational)     { CurrentDirectory.Set(tagId, rational);    }
        public void SetRationalArray(int tagId, Rational[] array) { CurrentDirectory.Set(tagId, array);       }
        public void SetFloat(int tagId, float float32)            { CurrentDirectory.Set(tagId, float32);     }
        public void SetFloatArray(int tagId, float[] array)       { CurrentDirectory.Set(tagId, array);       }
        public void SetDouble(int tagId, double double64)         { CurrentDirectory.Set(tagId, double64);    }
        public void SetDoubleArray(int tagId, double[] array)     { CurrentDirectory.Set(tagId, array);       }
        public void SetInt8S(int tagId, sbyte int8S)              { CurrentDirectory.Set(tagId, int8S);       }
        public void SetInt8SArray(int tagId, sbyte[] array)       { CurrentDirectory.Set(tagId, array);       }
        public void SetInt8U(int tagId, byte int8U)               { CurrentDirectory.Set(tagId, int8U);       }
        public void SetInt8UArray(int tagId, byte[] array)        { CurrentDirectory.Set(tagId, array);       }
        public void SetInt16S(int tagId, short int16S)            { CurrentDirectory.Set(tagId, int16S);      }
        public void SetInt16SArray(int tagId, short[] array)      { CurrentDirectory.Set(tagId, array);       }
        public void SetInt16U(int tagId, ushort int16U)           { CurrentDirectory.Set(tagId, int16U);      }
        public void SetInt16UArray(int tagId, ushort[] array)     { CurrentDirectory.Set(tagId, array);       }
        public void SetInt32S(int tagId, int int32S)              { CurrentDirectory.Set(tagId, int32S);      }
        public void SetInt32SArray(int tagId, int[] array)        { CurrentDirectory.Set(tagId, array);       }
        public void SetInt32U(int tagId, uint int32U)             { CurrentDirectory.Set(tagId, int32U);      }
        public void SetInt32UArray(int tagId, uint[] array)       { CurrentDirectory.Set(tagId, array);       }

        public abstract bool CustomProcessTag(int tagOffset, ICollection<int> processedIfdOffsets, IndexedReader reader, int tagId, int byteCount);

        public abstract bool TryCustomProcessFormat(int tagId, TiffDataFormatCode formatCode, uint componentCount, out long byteCount);

        public abstract bool HasFollowerIfd();

        public abstract bool TryEnterSubIfd(int tagType);

        public abstract void SetTiffMarker(int marker);
    }

    public enum TiffDataFormatCode : ushort
    {
        Int8U = 1,
        String = 2,
        Int16U = 3,
        Int32U = 4,
        RationalU = 5,
        Int8S = 6,
        Undefined = 7,
        Int16S = 8,
        Int32S = 9,
        RationalS = 10,
        Single = 11,
        Double = 12
    }

    /// <summary>An enumeration of data formats used by the TIFF specification.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class TiffDataFormat
    {
        public static readonly TiffDataFormat Int8U     = new TiffDataFormat("BYTE",      TiffDataFormatCode.Int8U,     1);
        public static readonly TiffDataFormat String    = new TiffDataFormat("STRING",    TiffDataFormatCode.String,    1);
        public static readonly TiffDataFormat Int16U    = new TiffDataFormat("USHORT",    TiffDataFormatCode.Int16U,    2);
        public static readonly TiffDataFormat Int32U    = new TiffDataFormat("ULONG",     TiffDataFormatCode.Int32U,    4);
        public static readonly TiffDataFormat RationalU = new TiffDataFormat("URATIONAL", TiffDataFormatCode.RationalU, 8);
        public static readonly TiffDataFormat Int8S     = new TiffDataFormat("SBYTE",     TiffDataFormatCode.Int8S,     1);
        public static readonly TiffDataFormat Undefined = new TiffDataFormat("UNDEFINED", TiffDataFormatCode.Undefined, 1);
        public static readonly TiffDataFormat Int16S    = new TiffDataFormat("SSHORT",    TiffDataFormatCode.Int16S,    2);
        public static readonly TiffDataFormat Int32S    = new TiffDataFormat("SLONG",     TiffDataFormatCode.Int32S,    4);
        public static readonly TiffDataFormat RationalS = new TiffDataFormat("SRATIONAL", TiffDataFormatCode.RationalS, 8);
        public static readonly TiffDataFormat Single    = new TiffDataFormat("SINGLE",    TiffDataFormatCode.Single,    4);
        public static readonly TiffDataFormat Double    = new TiffDataFormat("DOUBLE",    TiffDataFormatCode.Double,    8);

        [CanBeNull]
        public static TiffDataFormat FromTiffFormatCode(TiffDataFormatCode tiffFormatCode)
        {
            switch (tiffFormatCode)
            {
                case TiffDataFormatCode.Int8U:     return Int8U;
                case TiffDataFormatCode.String:    return String;
                case TiffDataFormatCode.Int16U:    return Int16U;
                case TiffDataFormatCode.Int32U:    return Int32U;
                case TiffDataFormatCode.RationalU: return RationalU;
                case TiffDataFormatCode.Int8S:     return Int8S;
                case TiffDataFormatCode.Undefined: return Undefined;
                case TiffDataFormatCode.Int16S:    return Int16S;
                case TiffDataFormatCode.Int32S:    return Int32S;
                case TiffDataFormatCode.RationalS: return RationalS;
                case TiffDataFormatCode.Single:    return Single;
                case TiffDataFormatCode.Double:    return Double;
            }

            return null;
        }

        [NotNull]
        public string Name { get { return _Name; } }
        string _Name;
        public int ComponentSizeBytes { get { return _ComponentSizeBytes; } }
        int _ComponentSizeBytes;
        public TiffDataFormatCode TiffFormatCode { get { return _TiffFormatCode; } }
        TiffDataFormatCode _TiffFormatCode;

        private TiffDataFormat([NotNull] string name, TiffDataFormatCode tiffFormatCode, int componentSizeBytes)
        {
            _Name = name;
            _TiffFormatCode = tiffFormatCode;
            _ComponentSizeBytes = componentSizeBytes;
        }

        public override string ToString() { return Name; }
    }

    /// <summary>
    /// Processes TIFF-formatted data, calling into client code via that <see cref="ITiffHandler"/> interface.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public static class TiffReader
    {
        /// <summary>Processes a TIFF data sequence.</summary>
        /// <param name="reader">the <see cref="IndexedReader"/> from which the data should be read</param>
        /// <param name="handler">the <see cref="ITiffHandler"/> that will coordinate processing and accept read values</param>
        /// <exception cref="TiffProcessingException">if an error occurred during the processing of TIFF data that could not be ignored or recovered from</exception>
        /// <exception cref="System.IO.IOException">an error occurred while accessing the required data</exception>
        /// <exception cref="TiffProcessingException"/>
        public static void ProcessTiff([NotNull] IndexedReader reader, [NotNull] ITiffHandler handler)
        {
            // Read byte order.
            var byteOrder = reader.GetInt16(0);
            switch (byteOrder)
            {
                case 0x4d4d: // MM
                    reader = reader.WithByteOrder(isMotorolaByteOrder: true);
                    break;
                case 0x4949: // II
                    reader = reader.WithByteOrder(isMotorolaByteOrder: false);
                    break;
                default:
                    throw new TiffProcessingException("Unclear distinction between Motorola/Intel byte ordering: " + reader.GetInt16(0));
            }

            // Check the next two values for correctness.
            int tiffMarker = reader.GetUInt16(2);
            handler.SetTiffMarker(tiffMarker);

            var firstIfdOffset = reader.GetInt32(4);

            // David Ekholm sent a digital camera image that has this problem
            // TODO calling Length should be avoided as it causes IndexedCapturingReader to read to the end of the stream
            if (firstIfdOffset >= reader.Length - 1)
            {
                handler.Warn("First IFD offset is beyond the end of the TIFF data segment -- trying default offset");
                // First directory normally starts immediately after the offset bytes, so try that
                firstIfdOffset = 2 + 2 + 4;
            }

            var processedIfdOffsets = new HashSet<int>();

            ProcessIfd(handler, reader, processedIfdOffsets, firstIfdOffset);
        }

        /// <summary>Processes a TIFF IFD.</summary>
        /// <remarks>
        /// IFD Header:
        /// <list type="bullet">
        ///   <item><b>2 bytes</b> number of tags</item>
        /// </list>
        /// Tag structure:
        /// <list type="bullet">
        ///   <item><b>2 bytes</b> tag type</item>
        ///   <item><b>2 bytes</b> format code (values 1 to 12, inclusive)</item>
        ///   <item><b>4 bytes</b> component count</item>
        ///   <item><b>4 bytes</b> inline value, or offset pointer if too large to fit in four bytes</item>
        /// </list>
        /// </remarks>
        /// <param name="handler">the <see cref="ITiffHandler"/> that will coordinate processing and accept read values</param>
        /// <param name="reader">the <see cref="IndexedReader"/> from which the data should be read</param>
        /// <param name="processedGlobalIfdOffsets">the set of visited IFD offsets, to avoid revisiting the same IFD in an endless loop</param>
        /// <param name="ifdOffset">the offset within <c>reader</c> at which the IFD data starts</param>
        /// <exception cref="System.IO.IOException">an error occurred while accessing the required data</exception>
        public static void ProcessIfd([NotNull] ITiffHandler handler, [NotNull] IndexedReader reader, [NotNull] ICollection<int> processedGlobalIfdOffsets, int ifdOffset)
        {
            try
            {
                // Check for directories we've already visited to avoid stack overflows when recursive/cyclic directory structures exist.
                // Note that we track these offsets in the global frame, not the reader's local frame.
                var globalIfdOffset = reader.ToUnshiftedOffset(ifdOffset);
                if (processedGlobalIfdOffsets.Contains(globalIfdOffset))
                    return;

                // Remember that we've visited this directory so that we don't visit it again later
                processedGlobalIfdOffsets.Add(globalIfdOffset);

                // Validate IFD offset
                if (ifdOffset >= reader.Length || ifdOffset < 0)
                {
                    handler.Error("Ignored IFD marked to start outside data segment");
                    return;
                }

                // First two bytes in the IFD are the number of tags in this directory
                int dirTagCount = reader.GetUInt16(ifdOffset);

                // Some software modifies the byte order of the file, but misses some IFDs (such as makernotes).
                // The entire test image repository doesn't contain a single IFD with more than 255 entries.
                // Here we detect switched bytes that suggest this problem, and temporarily swap the byte order.
                // This was discussed in GitHub issue #136.
                if (dirTagCount > 0xFF && (dirTagCount & 0xFF) == 0)
                {
                    dirTagCount >>= 8;
                    reader = reader.WithByteOrder(!reader.IsMotorolaByteOrder);
                }

                var dirLength = 2 + 12*dirTagCount + 4;
                if (dirLength + ifdOffset > reader.Length)
                {
                    handler.Error("Illegally sized IFD");
                    return;
                }

                //
                // Handle each tag in this directory
                //
                var invalidTiffFormatCodeCount = 0;
                for (var tagNumber = 0; tagNumber < dirTagCount; tagNumber++)
                {
                    var tagOffset = CalculateTagOffset(ifdOffset, tagNumber);

                    // 2 bytes for the tag id
                    int tagId = reader.GetUInt16(tagOffset);

                    // 2 bytes for the format code
                    var formatCode = (TiffDataFormatCode)reader.GetUInt16(tagOffset + 2);

                    // 4 bytes dictate the number of components in this tag's data
                    var componentCount = reader.GetUInt32(tagOffset + 4);

                    var format = TiffDataFormat.FromTiffFormatCode(formatCode);

                    long byteCount;
                    if (format == null)
                    {
                        if (!handler.TryCustomProcessFormat(tagId, formatCode, componentCount, out byteCount))
                        {
                            // This error suggests that we are processing at an incorrect index and will generate
                            // rubbish until we go out of bounds (which may be a while).  Exit now.
                            handler.Error(string.Format("Invalid TIFF tag format code {0} for tag 0x{1:X4}", formatCode, tagId));
                            // TODO specify threshold as a parameter, or provide some other external control over this behaviour
                            if (++invalidTiffFormatCodeCount > 5)
                            {
                                handler.Error("Stopping processing as too many errors seen in TIFF IFD");
                                return;
                            }
                            continue;
                        }
                    }
                    else
                    {
                        byteCount = componentCount * format.ComponentSizeBytes;
                    }

                    long tagValueOffset;
                    if (byteCount > 4)
                    {
                        // If it's bigger than 4 bytes, the dir entry contains an offset.
                        tagValueOffset = reader.GetUInt32(tagOffset + 8);
                        if (tagValueOffset + byteCount > reader.Length)
                        {
                            // Bogus pointer offset and / or byteCount value
                            handler.Error("Illegal TIFF tag pointer offset");
                            continue;
                        }
                    }
                    else
                    {
                        // 4 bytes or less and value is in the dir entry itself.
                        tagValueOffset = tagOffset + 8;
                    }

                    if (tagValueOffset < 0 || tagValueOffset > reader.Length)
                    {
                        handler.Error("Illegal TIFF tag pointer offset");
                        continue;
                    }

                    // Check that this tag isn't going to allocate outside the bounds of the data array.
                    // This addresses an uncommon OutOfMemoryError.
                    if (byteCount < 0 || tagValueOffset + byteCount > reader.Length)
                    {
                        handler.Error("Illegal number of bytes for TIFF tag data: " + byteCount);
                        continue;
                    }

                    // Some tags point to one or more additional IFDs to process
                    var isIfdPointer = false;
                    if (byteCount == checked(4L*componentCount))
                    {
                        for (var i = 0; i < componentCount; i++)
                        {
                            if (handler.TryEnterSubIfd(tagId))
                            {
                                isIfdPointer = true;
                                var subDirOffset = reader.GetUInt32((int)(tagValueOffset + i*4));
                                ProcessIfd(handler, reader, processedGlobalIfdOffsets, (int)subDirOffset);
                            }
                        }
                    }

                    // If it wasn't an IFD pointer, allow custom tag processing to occur
                    if (!isIfdPointer && !handler.CustomProcessTag((int)tagValueOffset, processedGlobalIfdOffsets, reader, tagId, (int)byteCount))
                    {
                        // If no custom processing occurred, process the tag in the standard fashion
                        ProcessTag(handler, tagId, (int)tagValueOffset, (int)componentCount, formatCode, reader);
                    }
                }

                // at the end of each IFD is an optional link to the next IFD
                var finalTagOffset = CalculateTagOffset(ifdOffset, dirTagCount);
                var nextIfdOffset = reader.GetInt32(finalTagOffset);
                if (nextIfdOffset != 0)
                {
                    if (nextIfdOffset >= reader.Length)
                    {
                        // Last 4 bytes of IFD reference another IFD with an address that is out of bounds
                        return;
                    }
                    else if (nextIfdOffset < ifdOffset)
                    {
                        // TODO is this a valid restriction?
                        // Last 4 bytes of IFD reference another IFD with an address that is before the start of this directory
                        return;
                    }

                    if (handler.HasFollowerIfd())
                        ProcessIfd(handler, reader, processedGlobalIfdOffsets, nextIfdOffset);
                }
            }
            finally
            {
                handler.EndingIfd();
            }
        }

        /// <exception cref="System.IO.IOException"/>
        private static void ProcessTag([NotNull] ITiffHandler handler, int tagId, int tagValueOffset, int componentCount, TiffDataFormatCode formatCode, [NotNull] IndexedReader reader)
        {
            switch (formatCode)
            {
                case TiffDataFormatCode.Undefined:
                {
                    // this includes exif user comments
                    handler.SetByteArray(tagId, reader.GetBytes(tagValueOffset, componentCount));
                    break;
                }
                case TiffDataFormatCode.String:
                {
                    handler.SetString(tagId, reader.GetNullTerminatedStringValue(tagValueOffset, componentCount));
                    break;
                }
                case TiffDataFormatCode.RationalS:
                {
                    if (componentCount == 1)
                    {
                        handler.SetRational(tagId, new Rational(reader.GetInt32(tagValueOffset), reader.GetInt32(tagValueOffset + 4)));
                    }
                    else if (componentCount > 1)
                    {
                        var array = new Rational[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = new Rational(reader.GetInt32(tagValueOffset + 8*i), reader.GetInt32(tagValueOffset + 4 + 8*i));
                        handler.SetRationalArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.RationalU:
                {
                    if (componentCount == 1)
                    {
                        handler.SetRational(tagId, new Rational(reader.GetUInt32(tagValueOffset), reader.GetUInt32(tagValueOffset + 4)));
                    }
                    else if (componentCount > 1)
                    {
                        var array = new Rational[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = new Rational(reader.GetUInt32(tagValueOffset + 8*i), reader.GetUInt32(tagValueOffset + 4 + 8*i));
                        handler.SetRationalArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.Single:
                {
                    if (componentCount == 1)
                    {
                        handler.SetFloat(tagId, reader.GetFloat32(tagValueOffset));
                    }
                    else
                    {
                        var array = new float[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = reader.GetFloat32(tagValueOffset + i*4);
                        handler.SetFloatArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.Double:
                {
                    if (componentCount == 1)
                    {
                        handler.SetDouble(tagId, reader.GetDouble64(tagValueOffset));
                    }
                    else
                    {
                        var array = new double[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = reader.GetDouble64(tagValueOffset + i*4);
                        handler.SetDoubleArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.Int8S:
                {
                    if (componentCount == 1)
                    {
                        handler.SetInt8S(tagId, reader.GetSByte(tagValueOffset));
                    }
                    else
                    {
                        var array = new sbyte[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = reader.GetSByte(tagValueOffset + i);
                        handler.SetInt8SArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.Int8U:
                {
                    if (componentCount == 1)
                    {
                        handler.SetInt8U(tagId, reader.GetByte(tagValueOffset));
                    }
                    else
                    {
                        var array = new byte[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = reader.GetByte(tagValueOffset + i);
                        handler.SetInt8UArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.Int16S:
                {
                    if (componentCount == 1)
                    {
                        handler.SetInt16S(tagId, reader.GetInt16(tagValueOffset));
                    }
                    else
                    {
                        var array = new short[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = reader.GetInt16(tagValueOffset + i*2);
                        handler.SetInt16SArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.Int16U:
                {
                    if (componentCount == 1)
                    {
                        handler.SetInt16U(tagId, reader.GetUInt16(tagValueOffset));
                    }
                    else
                    {
                        var array = new ushort[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = reader.GetUInt16(tagValueOffset + i*2);
                        handler.SetInt16UArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.Int32S:
                {
                    // NOTE 'long' in this case means 32 bit, not 64
                    if (componentCount == 1)
                    {
                        handler.SetInt32S(tagId, reader.GetInt32(tagValueOffset));
                    }
                    else
                    {
                        var array = new int[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = reader.GetInt32(tagValueOffset + i*4);
                        handler.SetInt32SArray(tagId, array);
                    }
                    break;
                }
                case TiffDataFormatCode.Int32U:
                {
                    // NOTE 'long' in this case means 32 bit, not 64
                    if (componentCount == 1)
                    {
                        handler.SetInt32U(tagId, reader.GetUInt32(tagValueOffset));
                    }
                    else
                    {
                        var array = new uint[componentCount];
                        for (var i = 0; i < componentCount; i++)
                            array[i] = reader.GetUInt32(tagValueOffset + i*4);
                        handler.SetInt32UArray(tagId, array);
                    }
                    break;
                }
                default:
                {
                    handler.Error(string.Format("Invalid TIFF tag format code {0} for tag 0x{1:X4}", formatCode, tagId));
                    break;
                }
            }
        }

        /// <summary>Determine the offset of a given tag within the specified IFD.</summary>
        /// <remarks>
        /// Add 2 bytes for the tag count.
        /// Each entry is 12 bytes.
        /// </remarks>
        /// <param name="ifdStartOffset">the offset at which the IFD starts</param>
        /// <param name="entryNumber">the zero-based entry number</param>
        private static int CalculateTagOffset(int ifdStartOffset, int entryNumber) { return ifdStartOffset + 2 + 12*entryNumber; }
    }
}

namespace MetadataExtractor.Formats.Exif
{
    /// <summary>
    /// Provides human-readable string representations of tag values stored in a <see cref="ExifSubIfdDirectory"/>.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ExifSubIfdDescriptor : ExifDescriptorBase<ExifSubIfdDirectory>
    {
        public ExifSubIfdDescriptor([NotNull] ExifSubIfdDirectory directory)
            : base(directory)
        {
        }
    }

    /// <summary>Describes Exif tags from the SubIFD directory.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ExifSubIfdDirectory : ExifDirectoryBase
    {
        /// <summary>This tag is a pointer to the Exif Interop IFD.</summary>
        public const int TagInteropOffset = 0xA005;

        public ExifSubIfdDirectory()
        {
            SetDescriptor(new ExifSubIfdDescriptor(this));
        }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>();

        static ExifSubIfdDirectory()
        {
            AddExifTagNames(_tagNameMap);
        }

        public override string Name { get { return "Exif SubIFD"; } }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    /// <summary>
    /// Provides human-readable string representations of tag values stored in a <see cref="PrintIMDirectory"/>.
    /// </summary>
    /// <author>Kevin Mott https://github.com/kwhopper</author>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class PrintIMDescriptor : TagDescriptor<PrintIMDirectory>
    {
        public PrintIMDescriptor([NotNull] PrintIMDirectory directory)
            : base(directory)
        {
        }

        public override string GetDescription(int tagType)
        {
            switch (tagType)
            {
                case PrintIMDirectory.TagPrintImVersion:
                    return base.GetDescription(tagType);
                default:
                    uint value;
                    if (!Directory.TryGetUInt32(tagType, out value))
                        return null;
                    return "0x" + value.ToString("x8");
            }
        }
    }

    /// <summary>Base class for several Exif format descriptor classes.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public abstract class ExifDescriptorBase<T> : TagDescriptor<T> where T : Directory
    {
        protected ExifDescriptorBase([NotNull] T directory)
            : base(directory)
        {
        }

        public override string GetDescription(int tagType)
        {
            // TODO order case blocks and corresponding methods in the same order as the TAG_* values are defined

            switch (tagType)
            {
                case ExifDirectoryBase.TagInteropIndex:
                    return GetInteropIndexDescription();
                case ExifDirectoryBase.TagInteropVersion:
                    return GetInteropVersionDescription();
                case ExifDirectoryBase.TagOrientation:
                    return GetOrientationDescription();
                case ExifDirectoryBase.TagResolutionUnit:
                    return GetResolutionDescription();
                case ExifDirectoryBase.TagYCbCrPositioning:
                    return GetYCbCrPositioningDescription();
                case ExifDirectoryBase.TagXResolution:
                    return GetXResolutionDescription();
                case ExifDirectoryBase.TagYResolution:
                    return GetYResolutionDescription();
                case ExifDirectoryBase.TagImageWidth:
                    return GetImageWidthDescription();
                case ExifDirectoryBase.TagImageHeight:
                    return GetImageHeightDescription();
                case ExifDirectoryBase.TagBitsPerSample:
                    return GetBitsPerSampleDescription();
                case ExifDirectoryBase.TagPhotometricInterpretation:
                    return GetPhotometricInterpretationDescription();
                case ExifDirectoryBase.TagRowsPerStrip:
                    return GetRowsPerStripDescription();
                case ExifDirectoryBase.TagStripByteCounts:
                    return GetStripByteCountsDescription();
                case ExifDirectoryBase.TagSamplesPerPixel:
                    return GetSamplesPerPixelDescription();
                case ExifDirectoryBase.TagPlanarConfiguration:
                    return GetPlanarConfigurationDescription();
                case ExifDirectoryBase.TagYCbCrSubsampling:
                    return GetYCbCrSubsamplingDescription();
                case ExifDirectoryBase.TagReferenceBlackWhite:
                    return GetReferenceBlackWhiteDescription();
                case ExifDirectoryBase.TagWinAuthor:
                    return GetWindowsAuthorDescription();
                case ExifDirectoryBase.TagWinComment:
                    return GetWindowsCommentDescription();
                case ExifDirectoryBase.TagWinKeywords:
                    return GetWindowsKeywordsDescription();
                case ExifDirectoryBase.TagWinSubject:
                    return GetWindowsSubjectDescription();
                case ExifDirectoryBase.TagWinTitle:
                    return GetWindowsTitleDescription();
                case ExifDirectoryBase.TagNewSubfileType:
                    return GetNewSubfileTypeDescription();
                case ExifDirectoryBase.TagSubfileType:
                    return GetSubfileTypeDescription();
                case ExifDirectoryBase.TagThresholding:
                    return GetThresholdingDescription();
                case ExifDirectoryBase.TagFillOrder:
                    return GetFillOrderDescription();
                case ExifDirectoryBase.TagCfaPattern2:
                    return GetCfaPattern2Description();
                case ExifDirectoryBase.TagExposureTime:
                    return GetExposureTimeDescription();
                case ExifDirectoryBase.TagShutterSpeed:
                    return GetShutterSpeedDescription();
                case ExifDirectoryBase.TagFNumber:
                    return GetFNumberDescription();
                case ExifDirectoryBase.TagCompressedAverageBitsPerPixel:
                    return GetCompressedAverageBitsPerPixelDescription();
                case ExifDirectoryBase.TagSubjectDistance:
                    return GetSubjectDistanceDescription();
                case ExifDirectoryBase.TagMeteringMode:
                    return GetMeteringModeDescription();
                case ExifDirectoryBase.TagWhiteBalance:
                    return GetWhiteBalanceDescription();
                case ExifDirectoryBase.TagFlash:
                    return GetFlashDescription();
                case ExifDirectoryBase.TagFocalLength:
                    return GetFocalLengthDescription();
                case ExifDirectoryBase.TagColorSpace:
                    return GetColorSpaceDescription();
                case ExifDirectoryBase.TagExifImageWidth:
                    return GetExifImageWidthDescription();
                case ExifDirectoryBase.TagExifImageHeight:
                    return GetExifImageHeightDescription();
                case ExifDirectoryBase.TagFocalPlaneResolutionUnit:
                    return GetFocalPlaneResolutionUnitDescription();
                case ExifDirectoryBase.TagFocalPlaneXResolution:
                    return GetFocalPlaneXResolutionDescription();
                case ExifDirectoryBase.TagFocalPlaneYResolution:
                    return GetFocalPlaneYResolutionDescription();
                case ExifDirectoryBase.TagExposureProgram:
                    return GetExposureProgramDescription();
                case ExifDirectoryBase.TagAperture:
                    return GetApertureValueDescription();
                case ExifDirectoryBase.TagMaxAperture:
                    return GetMaxApertureValueDescription();
                case ExifDirectoryBase.TagSensingMethod:
                    return GetSensingMethodDescription();
                case ExifDirectoryBase.TagExposureBias:
                    return GetExposureBiasDescription();
                case ExifDirectoryBase.TagFileSource:
                    return GetFileSourceDescription();
                case ExifDirectoryBase.TagSceneType:
                    return GetSceneTypeDescription();
                case ExifDirectoryBase.TagCfaPattern:
                    return GetCfaPatternDescription();
                case ExifDirectoryBase.TagComponentsConfiguration:
                    return GetComponentConfigurationDescription();
                case ExifDirectoryBase.TagExifVersion:
                    return GetExifVersionDescription();
                case ExifDirectoryBase.TagFlashpixVersion:
                    return GetFlashPixVersionDescription();
                case ExifDirectoryBase.TagIsoEquivalent:
                    return GetIsoEquivalentDescription();
                case ExifDirectoryBase.TagUserComment:
                    return GetUserCommentDescription();
                case ExifDirectoryBase.TagCustomRendered:
                    return GetCustomRenderedDescription();
                case ExifDirectoryBase.TagExposureMode:
                    return GetExposureModeDescription();
                case ExifDirectoryBase.TagWhiteBalanceMode:
                    return GetWhiteBalanceModeDescription();
                case ExifDirectoryBase.TagDigitalZoomRatio:
                    return GetDigitalZoomRatioDescription();
                case ExifDirectoryBase.Tag35MMFilmEquivFocalLength:
                    return Get35MMFilmEquivFocalLengthDescription();
                case ExifDirectoryBase.TagSceneCaptureType:
                    return GetSceneCaptureTypeDescription();
                case ExifDirectoryBase.TagGainControl:
                    return GetGainControlDescription();
                case ExifDirectoryBase.TagContrast:
                    return GetContrastDescription();
                case ExifDirectoryBase.TagSaturation:
                    return GetSaturationDescription();
                case ExifDirectoryBase.TagSharpness:
                    return GetSharpnessDescription();
                case ExifDirectoryBase.TagSubjectDistanceRange:
                    return GetSubjectDistanceRangeDescription();
                case ExifDirectoryBase.TagSensitivityType:
                    return GetSensitivityTypeDescription();
                case ExifDirectoryBase.TagCompression:
                    return GetCompressionDescription();
                case ExifDirectoryBase.TagJpegProc:
                    return GetJpegProcDescription();
                case ExifDirectoryBase.TagLensSpecification:
                    return GetLensSpecificationDescription();
                default:
                    return base.GetDescription(tagType);
            }
        }

        [CanBeNull]
        public string GetInteropVersionDescription()
        {
            return GetVersionBytesDescription(ExifDirectoryBase.TagInteropVersion, 2);
        }

        [CanBeNull]
        public string GetInteropIndexDescription()
        {
            var value = Directory.GetString(ExifDirectoryBase.TagInteropIndex);
            if (value == null)
                return null;
            return string.Equals("R98", value.Trim(), StringComparison.OrdinalIgnoreCase)
                ? "Recommended Exif Interoperability Rules (ExifR98)"
                : "Unknown (" + value + ")";
        }

        [CanBeNull]
        public string GetReferenceBlackWhiteDescription()
        {
            var ints = Directory.GetInt32Array(ExifDirectoryBase.TagReferenceBlackWhite);
            if (ints == null || ints.Length < 6)
                return null;
            var blackR = ints[0];
            var whiteR = ints[1];
            var blackG = ints[2];
            var whiteG = ints[3];
            var blackB = ints[4];
            var whiteB = ints[5];
            return "[" + blackR + "," + blackG + "," + blackB + "] [" + whiteR + "," + whiteG + "," + whiteB + "]";
        }

        [CanBeNull]
        public string GetYResolutionDescription()
        {
            var resolution = GetRationalOrDoubleString(ExifDirectoryBase.TagYResolution);
            if (resolution == null)
                return null;
            var unit = GetResolutionDescription();
            return resolution + " dots per " + (unit != null ? unit.ToLower() : "unit");
        }

        [CanBeNull]
        public string GetXResolutionDescription()
        {
            var resolution = GetRationalOrDoubleString(ExifDirectoryBase.TagXResolution);
            if (resolution == null)
                return null;
            var unit = GetResolutionDescription();
            return resolution + " dots per " + (unit != null ? unit.ToLower() : "unit");
        }

        [CanBeNull]
        public string GetYCbCrPositioningDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagYCbCrPositioning, 1,
                "Center of pixel array",
                "Datum point");
        }

        [CanBeNull]
        public string GetOrientationDescription()
        {
            return base.GetOrientationDescription(ExifDirectoryBase.TagOrientation);
        }

        [CanBeNull]
        public string GetResolutionDescription()
        {
            // '1' means no-unit, '2' means inch, '3' means centimeter. Default value is '2'(inch)
            return GetIndexedDescription(ExifDirectoryBase.TagResolutionUnit, 1,
                "(No unit)",
                "Inch",
                "cm");
        }

        /// <summary>The Windows specific tags uses plain Unicode.</summary>
        [CanBeNull]
        private string GetUnicodeDescription(int tag)
        {
            var bytes = Directory.GetByteArray(tag);
            if (bytes == null)
                return null;
            try
            {
                // Decode the Unicode string and trim the Unicode zero "\0" from the end.
                return Encoding.Unicode.GetString(bytes, 0, bytes.Length).TrimEnd('\0');
            }
            catch
            {
                return null;
            }
        }

        [CanBeNull]
        public string GetWindowsAuthorDescription()
        {
            return GetUnicodeDescription(ExifDirectoryBase.TagWinAuthor);
        }

        [CanBeNull]
        public string GetWindowsCommentDescription()
        {
            return GetUnicodeDescription(ExifDirectoryBase.TagWinComment);
        }

        [CanBeNull]
        public string GetWindowsKeywordsDescription()
        {
            return GetUnicodeDescription(ExifDirectoryBase.TagWinKeywords);
        }

        [CanBeNull]
        public string GetWindowsTitleDescription()
        {
            return GetUnicodeDescription(ExifDirectoryBase.TagWinTitle);
        }

        [CanBeNull]
        public string GetWindowsSubjectDescription()
        {
            return GetUnicodeDescription(ExifDirectoryBase.TagWinSubject);
        }

        [CanBeNull]
        public string GetYCbCrSubsamplingDescription()
        {
            var positions = Directory.GetInt32Array(ExifDirectoryBase.TagYCbCrSubsampling);
            if (positions == null || positions.Length < 2)
                return null;
            if (positions[0] == 2 && positions[1] == 1)
                return "YCbCr4:2:2";
            if (positions[0] == 2 && positions[1] == 2)
                return "YCbCr4:2:0";
            return "(Unknown)";
        }

        [CanBeNull]
        public string GetPlanarConfigurationDescription()
        {
            // When image format is no compression YCbCr, this value shows byte aligns of YCbCr
            // data. If value is '1', Y/Cb/Cr value is chunky format, contiguous for each subsampling
            // pixel. If value is '2', Y/Cb/Cr value is separated and stored to Y plane/Cb plane/Cr
            // plane format.
            return GetIndexedDescription(ExifDirectoryBase.TagPlanarConfiguration, 1, "Chunky (contiguous for each subsampling pixel)", "Separate (Y-plane/Cb-plane/Cr-plane format)");
        }

        [CanBeNull]
        public string GetSamplesPerPixelDescription()
        {
            var value = Directory.GetString(ExifDirectoryBase.TagSamplesPerPixel);
            return value == null ? null : value + " samples/pixel";
        }

        [CanBeNull]
        public string GetRowsPerStripDescription()
        {
            var value = Directory.GetString(ExifDirectoryBase.TagRowsPerStrip);
            return value == null ? null : value + " rows/strip";
        }

        [CanBeNull]
        public string GetStripByteCountsDescription()
        {
            var value = Directory.GetString(ExifDirectoryBase.TagStripByteCounts);
            return value == null ? null : value + " bytes";
        }

        [CanBeNull]
        public string GetPhotometricInterpretationDescription()
        {
            // Shows the color space of the image data components
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagPhotometricInterpretation, out value))
                return null;

            switch (value)
            {
                case 0:
                    return "WhiteIsZero";
                case 1:
                    return "BlackIsZero";
                case 2:
                    return "RGB";
                case 3:
                    return "RGB Palette";
                case 4:
                    return "Transparency Mask";
                case 5:
                    return "CMYK";
                case 6:
                    return "YCbCr";
                case 8:
                    return "CIELab";
                case 9:
                    return "ICCLab";
                case 10:
                    return "ITULab";
                case 32803:
                    return "Color Filter Array";
                case 32844:
                    return "Pixar LogL";
                case 32845:
                    return "Pixar LogLuv";
                case 32892:
                    return "Linear Raw";
                default:
                    return "Unknown colour space";
            }
        }

        [CanBeNull]
        public string GetBitsPerSampleDescription()
        {
            var value = Directory.GetString(ExifDirectoryBase.TagBitsPerSample);
            return value == null ? null : value + " bits/component/pixel";
        }

        [CanBeNull]
        public string GetImageWidthDescription()
        {
            var value = Directory.GetString(ExifDirectoryBase.TagImageWidth);
            return value == null ? null : value + " pixels";
        }

        [CanBeNull]
        public string GetImageHeightDescription()
        {
            var value = Directory.GetString(ExifDirectoryBase.TagImageHeight);
            return value == null ? null : value + " pixels";
        }

        [CanBeNull]
        public string GetNewSubfileTypeDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagNewSubfileType, 0,
                "Full-resolution image",
                "Reduced-resolution image",
                "Single page of multi-page image",
                "Single page of multi-page reduced-resolution image",
                "Transparency mask",
                "Transparency mask of reduced-resolution image",
                "Transparency mask of multi-page image",
                "Transparency mask of reduced-resolution multi-page image");
        }

        [CanBeNull]
        public string GetSubfileTypeDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagSubfileType, 1,
                "Full-resolution image",
                "Reduced-resolution image",
                "Single page of multi-page image");
        }

        [CanBeNull]
        public string GetThresholdingDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagThresholding, 1,
                "No dithering or halftoning",
                "Ordered dither or halftone",
                "Randomized dither");
        }

        [CanBeNull]
        public string GetFillOrderDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagFillOrder, 1,
                "Normal",
                "Reversed");
        }

        [CanBeNull]
        public string GetSubjectDistanceRangeDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagSubjectDistanceRange,
                "Unknown",
                "Macro",
                "Close view",
                "Distant view");
        }

        [CanBeNull]
        public string GetSensitivityTypeDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagSensitivityType,
                "Unknown",
                "Standard Output Sensitivity",
                "Recommended Exposure Index",
                "ISO Speed",
                "Standard Output Sensitivity and Recommended Exposure Index",
                "Standard Output Sensitivity and ISO Speed",
                "Recommended Exposure Index and ISO Speed",
                "Standard Output Sensitivity, Recommended Exposure Index and ISO Speed");
        }

        [CanBeNull]
        public string GetLensSpecificationDescription()
        {
            return GetLensSpecificationDescription(ExifDirectoryBase.TagLensSpecification);
        }

        [CanBeNull]
        public string GetSharpnessDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagSharpness,
                "None",
                "Low",
                "Hard");
        }

        [CanBeNull]
        public string GetSaturationDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagSaturation,
                "None",
                "Low saturation",
                "High saturation");
        }

        [CanBeNull]
        public string GetContrastDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagContrast,
                "None",
                "Soft",
                "Hard");
        }

        [CanBeNull]
        public string GetGainControlDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagGainControl,
                "None",
                "Low gain up",
                "Low gain down",
                "High gain up",
                "High gain down");
        }

        [CanBeNull]
        public string GetSceneCaptureTypeDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagSceneCaptureType,
                "Standard",
                "Landscape",
                "Portrait",
                "Night scene");
        }

        [CanBeNull]
        public string Get35MMFilmEquivFocalLengthDescription()
        {
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.Tag35MMFilmEquivFocalLength, out value))
                return null;
            return value == 0 ? "Unknown" : GetFocalLengthDescription(value);
        }

        [CanBeNull]
        public string GetDigitalZoomRatioDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(ExifDirectoryBase.TagDigitalZoomRatio, out value))
                return null;
            return value.Numerator == 0
                ? "Digital zoom not used"
                : value.ToDouble().ToString("0.#");
        }

        [CanBeNull]
        public string GetWhiteBalanceModeDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagWhiteBalanceMode,
                "Auto white balance",
                "Manual white balance");
        }

        [CanBeNull]
        public string GetExposureModeDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagExposureMode,
                "Auto exposure",
                "Manual exposure",
                "Auto bracket");
        }

        [CanBeNull]
        public string GetCustomRenderedDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagCustomRendered,
                "Normal process",
                "Custom process");
        }

        [CanBeNull]
        public string GetUserCommentDescription()
        {
            var commentBytes = Directory.GetByteArray(ExifDirectoryBase.TagUserComment);

            if (commentBytes == null)
                return null;

            if (commentBytes.Length == 0)
                return string.Empty;

            // TODO use ByteTrie here
            // Someone suggested "ISO-8859-1".
            var encodingMap = new Dictionary<string, Encoding>
            {
                { "ASCII", Encoding.ASCII },
                { "UTF8", Encoding.UTF8 },
                { "UTF7", Encoding.UTF7 },
                { "UTF32", Encoding.UTF32 },
                { "UNICODE", Encoding.Unicode },
                { "JIS", Encoding.GetEncoding("Shift-JIS") }
            };

            try
            {
                if (commentBytes.Length >= 10)
                {
                    // TODO no guarantee bytes after the UTF8 name are valid UTF8 -- only read as many as needed
                    var firstTenBytesString = Encoding.UTF8.GetString(commentBytes, 0, 10);
                    // try each encoding name
                    foreach (var pair in encodingMap)
                    {
                        var encodingName = pair.Key;
                        var encoding = pair.Value;
                        if (firstTenBytesString.StartsWith(encodingName))
                        {
                            // skip any null or blank characters commonly present after the encoding name, up to a limit of 10 from the start
                            for (var j = encodingName.Length; j < 10; j++)
                            {
                                var b = commentBytes[j];
                                if (b != '\0' && b != ' ')
                                {
                                    return encoding.GetString(commentBytes, j, commentBytes.Length - j).Trim('\0', ' ');
                                }
                            }
                            return encoding.GetString(commentBytes, 10, commentBytes.Length - 10).Trim('\0', ' ');
                        }
                    }
                }
                // special handling fell through, return a plain string representation
                return Encoding.UTF8.GetString(commentBytes, 0, commentBytes.Length).Trim('\0', ' ');
            }
            catch
            {
                return null;
            }
        }

        [CanBeNull]
        public string GetIsoEquivalentDescription()
        {
            // Have seen an exception here from files produced by ACDSEE that stored an int[] here with two values
            // There used to be a check here that multiplied ISO values < 50 by 200.
            // Issue 36 shows a smart-phone image from a Samsung Galaxy S2 with ISO-40.
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagIsoEquivalent, out value))
                return null;
            return value.ToString();
        }

        [CanBeNull]
        public string GetExifVersionDescription()
        {
            return GetVersionBytesDescription(ExifDirectoryBase.TagExifVersion, 2);
        }

        [CanBeNull]
        public string GetFlashPixVersionDescription()
        {
            return GetVersionBytesDescription(ExifDirectoryBase.TagFlashpixVersion, 2);
        }

        [CanBeNull]
        public string GetSceneTypeDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagSceneType, 1,
                "Directly photographed image");
        }

        /// <summary>
        /// String description of CFA Pattern
        /// </summary>
        /// <remarks>
        /// Converted from Exiftool version 10.33 created by Phil Harvey
        /// http://www.sno.phy.queensu.ca/~phil/exiftool/
        /// lib\Image\ExifTool\Exif.pm
        ///
        /// Indicates the color filter array (CFA) geometric pattern of the image sensor when a one-chip color area sensor is used.
        /// It does not apply to all sensing methods.
        /// </remarks>
        [CanBeNull]
        public string GetCfaPatternDescription()
        {
            return FormatCFAPattern(DecodeCFAPattern(ExifDirectoryBase.TagCfaPattern));
        }

        /// <summary>
        /// String description of CFA Pattern
        /// </summary>
        /// <remarks>
        /// Indicates the color filter array (CFA) geometric pattern of the image sensor when a one-chip color area sensor is used.
        /// It does not apply to all sensing methods.
        ///
        /// <see cref="ExifDirectoryBase.TagCfaPattern2"/> holds only the pixel pattern. <see cref="ExifDirectoryBase.TagCfaRepeatPatternDim"/> is expected to exist and pass
        /// some conditional tests.
        /// </remarks>
        [CanBeNull]
        public string GetCfaPattern2Description()
        {
            var values = Directory.GetByteArray(ExifDirectoryBase.TagCfaPattern2);
            if (values == null)
                return null;

            var repeatPattern = Directory.GetObject(ExifDirectoryBase.TagCfaRepeatPatternDim) as ushort[];
            if (repeatPattern == null)
                return "Repeat Pattern not found for CFAPattern (" + base.GetDescription(ExifDirectoryBase.TagCfaPattern2) + ")";

            if (repeatPattern.Length == 2 && values.Length == (repeatPattern[0] * repeatPattern[1]))
            {
                var intpattern = new int[2 + values.Length];
                intpattern[0] = repeatPattern[0];
                intpattern[1] = repeatPattern[1];

                Array.Copy(values, 0, intpattern, 2, values.Length);

                return FormatCFAPattern(intpattern);
            }

            return "Unknown Pattern (" + base.GetDescription(ExifDirectoryBase.TagCfaPattern2) + ")";
        }

        [CanBeNull]
        private static string FormatCFAPattern(int[] pattern)
        {
            if (pattern.Length < 2)
                return "<truncated data>";
            if (pattern[0] == 0 && pattern[1] == 0)
                return "<zero pattern size>";

            var end = 2 + pattern[0] * pattern[1];
            if (end > pattern.Length)
                return "<invalid pattern size>";

            string[] cfaColors = { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow", "White" };

            var ret = new StringBuilder();
            ret.Append("[");
            for (var pos = 2; pos < end; pos++)
            {
                if (pattern[pos] <= cfaColors.Length - 1)
                    ret.Append(cfaColors[pattern[pos]]);
                else
                    ret.Append("Unknown");  // indicated pattern position is outside the array bounds

                if ((pos - 2) % pattern[1] == 0)
                    ret.Append(",");
                else if (pos != end - 1)
                    ret.Append("][");
            }
            ret.Append("]");

            return ret.ToString();
        }

        /// <summary>
        /// Decode raw CFAPattern value
        /// </summary>
        /// <remarks>
        /// Converted from Exiftool version 10.33 created by Phil Harvey
        /// http://www.sno.phy.queensu.ca/~phil/exiftool/
        /// lib\Image\ExifTool\Exif.pm
        ///
        /// The value consists of:
        /// - Two short, being the grid width and height of the repeated pattern.
        /// - Next, for every pixel in that pattern, an identification code.
        /// </remarks>
        private int[] DecodeCFAPattern(int tagType)
        {
            int[] ret;

            var values = Directory.GetByteArray(tagType);
            if (values == null)
                return null;

            if (values.Length < 4)
            {
                ret = new int[values.Length];
                for (var i = 0; i < values.Length; i++)
                    ret[i] = values[i];
                return ret;
            }

            IndexedReader reader = new ByteArrayReader(values);

            // first two values should be read as 16-bits (2 bytes)
            var item0 = reader.GetInt16(0);
            var item1 = reader.GetInt16(2);

            ret = new int[values.Length - 2];

            var copyArray = false;
            var end = 2 + item0 * item1;
            if (end > values.Length) // sanity check in case of byte order problems; calculated 'end' should be <= length of the values
            {
                // try swapping byte order (I have seen this order different than in EXIF)
                reader = reader.WithByteOrder(!reader.IsMotorolaByteOrder);
                item0 = reader.GetInt16(0);
                item1 = reader.GetInt16(2);

                if (values.Length >= 2 + item0 * item1)
                    copyArray = true;
            }
            else
            {
                copyArray = true;
            }

            if (copyArray)
            {
                ret[0] = item0;
                ret[1] = item1;

                for (var i = 4; i < values.Length; i++)
                    ret[i - 2] = reader.GetByte(i);
            }
            return ret;
        }

        [CanBeNull]
        public string GetFileSourceDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagFileSource, 1,
                "Film Scanner",
                "Reflection Print Scanner",
                "Digital Still Camera (DSC)");
        }

        [CanBeNull]
        public string GetExposureBiasDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(ExifDirectoryBase.TagExposureBias, out value))
                return null;
            return value.ToSimpleString() + " EV";
        }

        [CanBeNull]
        public string GetMaxApertureValueDescription()
        {
            double aperture;
            if (!Directory.TryGetDouble(ExifDirectoryBase.TagMaxAperture, out aperture))
                return null;
            return GetFStopDescription(PhotographicConversions.ApertureToFStop(aperture));
        }

        [CanBeNull]
        public string GetApertureValueDescription()
        {
            double aperture;
            if (!Directory.TryGetDouble(ExifDirectoryBase.TagAperture, out aperture))
                return null;
            return GetFStopDescription(PhotographicConversions.ApertureToFStop(aperture));
        }

        [CanBeNull]
        public string GetExposureProgramDescription()
        {
            return GetIndexedDescription(ExifDirectoryBase.TagExposureProgram, 1,
                "Manual control",
                "Program normal",
                "Aperture priority",
                "Shutter priority",
                "Program creative (slow program)",
                "Program action (high-speed program)",
                "Portrait mode",
                "Landscape mode");
        }

        [CanBeNull]
        public string GetFocalPlaneXResolutionDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(ExifDirectoryBase.TagFocalPlaneXResolution, out value))
                return null;
            var unit = GetFocalPlaneResolutionUnitDescription();
            return value.Reciprocal.ToSimpleString() + (unit == null ? string.Empty : " " + unit.ToLower());
        }

        [CanBeNull]
        public string GetFocalPlaneYResolutionDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(ExifDirectoryBase.TagFocalPlaneYResolution, out value))
                return null;
            var unit = GetFocalPlaneResolutionUnitDescription();
            return value.Reciprocal.ToSimpleString() + (unit == null ? string.Empty : " " + unit.ToLower());
        }

        [CanBeNull]
        public string GetFocalPlaneResolutionUnitDescription()
        {
            // Unit of FocalPlaneXResolution/FocalPlaneYResolution.
            // '1' means no-unit, '2' inch, '3' centimeter.
            return GetIndexedDescription(ExifDirectoryBase.TagFocalPlaneResolutionUnit, 1,
                "(No unit)",
                "Inches",
                "cm");
        }

        [CanBeNull]
        public string GetExifImageWidthDescription()
        {
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out value))
                return null;
            return value + " pixels";
        }

        [CanBeNull]
        public string GetExifImageHeightDescription()
        {
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out value))
                return null;
            return value + " pixels";
        }

        [CanBeNull]
        public string GetColorSpaceDescription()
        {
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagColorSpace, out value))
                return null;
            if (value == 1)
                return "sRGB";
            if (value == 65535)
                return "Undefined";
            return "Unknown (" + value + ")";
        }

        [CanBeNull]
        public string GetFocalLengthDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(ExifDirectoryBase.TagFocalLength, out value))
                return null;
            return GetFocalLengthDescription(value.ToDouble());
        }

        [CanBeNull]
        public string GetFlashDescription()
        {
            /*
             * This is a bit mask.
             * 0 = flash fired
             * 1 = return detected
             * 2 = return able to be detected
             * 3 = unknown
             * 4 = auto used
             * 5 = unknown
             * 6 = red eye reduction used
             */
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagFlash, out value))
                return null;

            var sb = new StringBuilder();
            sb.Append((value & 0x1) != 0 ? "Flash fired" : "Flash did not fire");
            // check if we're able to detect a return, before we mention it
            if ((value & 0x4) != 0)
                sb.Append((value & 0x2) != 0 ? ", return detected" : ", return not detected");
            if ((value & 0x10) != 0)
                sb.Append(", auto");
            if ((value & 0x40) != 0)
                sb.Append(", red-eye reduction");
            return sb.ToString();
        }

        [CanBeNull]
        public string GetWhiteBalanceDescription()
        {
            // See http://web.archive.org/web/20131018091152/http://exif.org/Exif2-2.PDF page 35

            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagWhiteBalance, out value))
                return null;

            switch (value)
            {
                case 0: return "Unknown";
                case 1: return "Daylight";
                case 2: return "Florescent";
                case 3: return "Tungsten";
                case 4: return "Flash";
                case 9: return "Fine Weather";
                case 10: return "Cloudy";
                case 11: return "Shade";
                case 12: return "Daylight Fluorescent";
                case 13: return "Day White Fluorescent";
                case 14: return "Cool White Fluorescent";
                case 15: return "White Fluorescent";
                case 16: return "Warm White Fluorescent";
                case 17: return "Standard light";
                case 18: return "Standard light (B)";
                case 19: return "Standard light (C)";
                case 20: return "D55";
                case 21: return "D65";
                case 22: return "D75";
                case 23: return "D50";
                case 24: return "Studio Tungsten";
                case 255: return "(Other)";
                default:
                    return "Unknown (" + value + ")";
            }
        }

        [CanBeNull]
        public string GetMeteringModeDescription()
        {
            // '0' means unknown, '1' average, '2' center weighted average, '3' spot
            // '4' multi-spot, '5' multi-segment, '6' partial, '255' other
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagMeteringMode, out value))
                return null;

            switch (value)
            {
                case 0:
                    return "Unknown";
                case 1:
                    return "Average";
                case 2:
                    return "Center weighted average";
                case 3:
                    return "Spot";
                case 4:
                    return "Multi-spot";
                case 5:
                    return "Multi-segment";
                case 6:
                    return "Partial";
                case 255:
                    return "(Other)";
                default:
                    return "Unknown (" + value + ")";
            }
        }

        [CanBeNull]
        public string GetCompressionDescription()
        {
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagCompression, out value))
                return null;

            switch (value)
            {
                case 1:
                    return "Uncompressed";
                case 2:
                    return "CCITT 1D";
                case 3:
                    return "T4/Group 3 Fax";
                case 4:
                    return "T6/Group 4 Fax";
                case 5:
                    return "LZW";
                case 6:
                    return "JPEG (old-style)";
                case 7:
                    return "JPEG";
                case 8:
                    return "Adobe Deflate";
                case 9:
                    return "JBIG B&W";
                case 10:
                    return "JBIG Color";
                case 99:
                    return "JPEG";
                case 262:
                    return "Kodak 262";
                case 32766:
                    return "Next";
                case 32767:
                    return "Sony ARW Compressed";
                case 32769:
                    return "Packed RAW";
                case 32770:
                    return "Samsung SRW Compressed";
                case 32771:
                    return "CCIRLEW";
                case 32772:
                    return "Samsung SRW Compressed 2";
                case 32773:
                    return "PackBits";
                case 32809:
                    return "Thunderscan";
                case 32867:
                    return "Kodak KDC Compressed";
                case 32895:
                    return "IT8CTPAD";
                case 32896:
                    return "IT8LW";
                case 32897:
                    return "IT8MP";
                case 32898:
                    return "IT8BL";
                case 32908:
                    return "PixarFilm";
                case 32909:
                    return "PixarLog";
                case 32946:
                    return "Deflate";
                case 32947:
                    return "DCS";
                case 34661:
                    return "JBIG";
                case 34676:
                    return "SGILog";
                case 34677:
                    return "SGILog24";
                case 34712:
                    return "JPEG 2000";
                case 34713:
                    return "Nikon NEF Compressed";
                case 34715:
                    return "JBIG2 TIFF FX";
                case 34718:
                    return "Microsoft Document Imaging (MDI) Binary Level Codec";
                case 34719:
                    return "Microsoft Document Imaging (MDI) Progressive Transform Codec";
                case 34720:
                    return "Microsoft Document Imaging (MDI) Vector";
                case 34892:
                    return "Lossy JPEG";
                case 65000:
                    return "Kodak DCR Compressed";
                case 65535:
                    return "Pentax PEF Compressed";
                default:
                    return "Unknown (" + value + ")";
            }
        }

        [CanBeNull]
        public string GetSubjectDistanceDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(ExifDirectoryBase.TagSubjectDistance, out value))
                return null;
            return string.Format("{0:0.0##} metres", value.ToDouble());
        }

        [CanBeNull]
        public string GetCompressedAverageBitsPerPixelDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(ExifDirectoryBase.TagCompressedAverageBitsPerPixel, out value))
                return null;
            var ratio = value.ToSimpleString();
            return value.IsInteger && value.ToInt32() == 1 ? ratio + " bit/pixel" : ratio + " bits/pixel";
        }

        [CanBeNull]
        public string GetExposureTimeDescription()
        {
            var value = Directory.GetString(ExifDirectoryBase.TagExposureTime);
            return value == null ? null : value + " sec";
        }

        [CanBeNull]
        public string GetShutterSpeedDescription()
        {
            return GetShutterSpeedDescription(ExifDirectoryBase.TagShutterSpeed);
        }

        [CanBeNull]
        public string GetFNumberDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(ExifDirectoryBase.TagFNumber, out value))
                return null;
            return GetFStopDescription(value.ToDouble());
        }

        [CanBeNull]
        public string GetSensingMethodDescription()
        {
            // '1' Not defined, '2' One-chip color area sensor, '3' Two-chip color area sensor
            // '4' Three-chip color area sensor, '5' Color sequential area sensor
            // '7' Trilinear sensor '8' Color sequential linear sensor,  'Other' reserved
            return GetIndexedDescription(ExifDirectoryBase.TagSensingMethod, 1,
                "(Not defined)",
                "One-chip color area sensor",
                "Two-chip color area sensor",
                "Three-chip color area sensor",
                "Color sequential area sensor",
                null,
                "Trilinear sensor",
                "Color sequential linear sensor");
        }

        [CanBeNull]
        public string GetComponentConfigurationDescription()
        {
            var components = Directory.GetInt32Array(ExifDirectoryBase.TagComponentsConfiguration);
            if (components == null)
                return null;
            var componentStrings = new[] { string.Empty, "Y", "Cb", "Cr", "R", "G", "B" };
            var componentConfig = new StringBuilder();
            for (var i = 0; i < Math.Min(4, components.Length); i++)
            {
                var j = components[i];
                if (j > 0 && j < componentStrings.Length)
                    componentConfig.Append(componentStrings[j]);
            }
            return componentConfig.ToString();
        }

        [CanBeNull]
        public string GetJpegProcDescription()
        {
            int value;
            if (!Directory.TryGetInt32(ExifDirectoryBase.TagJpegProc, out value))
                return null;

            switch (value)
            {
                case 1:
                    return "Baseline";
                case 14:
                    return "Lossless";
                default:
                    return "Unknown (" + value + ")";
            }
        }
    }

    /// <remarks>These tags can be found in Epson proprietary metadata. The index values are 'fake' but
    /// chosen specifically to make processing easier</remarks>
    /// <author>Kevin Mott https://github.com/kwhopper</author>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class PrintIMDirectory : Directory
    {
        public const int TagPrintImVersion = 0x0000;

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagPrintImVersion, "PrintIM Version" }
        };

        public PrintIMDirectory()
        {
            SetDescriptor(new PrintIMDescriptor(this));
        }

        public override string Name { get { return "PrintIM"; } }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    /// <summary>Base class for several Exif format tag directories.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public abstract class ExifDirectoryBase : Directory
    {
        public const int TagInteropIndex = 0x0001;

        public const int TagInteropVersion = 0x0002;

        /// <summary>The new subfile type tag.</summary>
        /// <remarks>
        /// 0 = Full-resolution Image
        /// 1 = Reduced-resolution image
        /// 2 = Single page of multi-page image
        /// 3 = Single page of multi-page reduced-resolution image
        /// 4 = Transparency mask
        /// 5 = Transparency mask of reduced-resolution image
        /// 6 = Transparency mask of multi-page image
        /// 7 = Transparency mask of reduced-resolution multi-page image
        /// </remarks>
        public const int TagNewSubfileType = 0x00FE;

        /// <summary>The old subfile type tag.</summary>
        /// <remarks>
        /// 1 = Full-resolution image (Main image)
        /// 2 = Reduced-resolution image (Thumbnail)
        /// 3 = Single page of multi-page image
        /// </remarks>
        public const int TagSubfileType = 0x00FF;

        public const int TagImageWidth = 0x0100;

        public const int TagImageHeight = 0x0101;

        /// <summary>
        /// When image format is no compression, this value shows the number of bits
        /// per component for each pixel.
        /// </summary>
        /// <remarks>
        /// Usually this value is '8,8,8'.
        /// </remarks>
        public const int TagBitsPerSample = 0x0102;

        public const int TagCompression = 0x0103;

        /// <summary>Shows the color space of the image data components.</summary>
        /// <remarks>
        /// 0 = WhiteIsZero
        /// 1 = BlackIsZero
        /// 2 = RGB
        /// 3 = RGB Palette
        /// 4 = Transparency Mask
        /// 5 = CMYK
        /// 6 = YCbCr
        /// 8 = CIELab
        /// 9 = ICCLab
        /// 10 = ITULab
        /// 32803 = Color Filter Array
        /// 32844 = Pixar LogL
        /// 32845 = Pixar LogLuv
        /// 34892 = Linear Raw
        /// </remarks>
        public const int TagPhotometricInterpretation = 0x0106;

        /// <summary>
        /// 1 = No dithering or halftoning
        /// 2 = Ordered dither or halftone
        /// 3 = Randomized dither
        /// </summary>
        public const int TagThresholding = 0x0107;

        /// <summary>
        /// 1 = Normal
        /// 2 = Reversed
        /// </summary>
        public const int TagFillOrder = 0x010A;

        public const int TagDocumentName = 0x010D;

        public const int TagImageDescription = 0x010E;

        public const int TagMake = 0x010F;

        public const int TagModel = 0x0110;

        /// <summary>The position in the file of raster data.</summary>
        public const int TagStripOffsets = 0x0111;

        public const int TagOrientation = 0x0112;

        /// <summary>Each pixel is composed of this many samples.</summary>
        public const int TagSamplesPerPixel = 0x0115;

        /// <summary>The raster is codified by a single block of data holding this many rows.</summary>
        public const int TagRowsPerStrip = 0x0116;

        /// <summary>The size of the raster data in bytes.</summary>
        public const int TagStripByteCounts = 0x0117;

        public const int TagMinSampleValue = 0x0118;

        public const int TagMaxSampleValue = 0x0119;

        public const int TagXResolution = 0x011A;

        public const int TagYResolution = 0x011B;

        /// <summary>
        /// When image format is no compression YCbCr, this value shows byte aligns of YCbCr data.
        /// </summary>
        /// <remarks>
        /// If value is '1', Y/Cb/Cr value is chunky format, contiguous for
        /// each subsampling pixel. If value is '2', Y/Cb/Cr value is separated and
        /// stored to Y plane/Cb plane/Cr plane format.
        /// </remarks>
        public const int TagPlanarConfiguration = 0x011C;

        public const int TagPageName = 0x011D;

        public const int TagResolutionUnit = 0x0128;
        public const int TagPageNumber = 0x0129;

        public const int TagTransferFunction = 0x012D;

        public const int TagSoftware = 0x0131;

        public const int TagDateTime = 0x0132;

        public const int TagArtist = 0x013B;

        public const int TagHostComputer = 0x013C;

        public const int TagPredictor = 0x013D;

        public const int TagWhitePoint = 0x013E;

        public const int TagPrimaryChromaticities = 0x013F;

        public const int TagTileWidth = 0x0142;

        public const int TagTileLength = 0x0143;

        public const int TagTileOffsets = 0x0144;

        public const int TagTileByteCounts = 0x0145;

        /// <summary>Tag is a pointer to one or more sub-IFDs.</summary>
        /// <remarks>Seems to be used exclusively by raw formats, referencing one or two IFDs.</remarks>
        public const int TagSubIfdOffset = 0x014a;

        public const int TagTransferRange = 0x0156;

        public const int TagJpegTables = 0x015B;

        public const int TagJpegProc = 0x0200;

        // 0x0201 can have all kinds of descriptions for thumbnail starting index
        // 0x0202 can have all kinds of descriptions for thumbnail length
        public const int TagJpegRestartInterval = 0x0203;
        public const int TagJpegLosslessPredictors = 0x0205;
        public const int TagJpegPointTransforms = 0x0206;
        public const int TagJpegQTables = 0x0207;
        public const int TagJpegDcTables = 0x0208;
        public const int TagJpegAcTables = 0x0209;

        public const int TagYCbCrCoefficients = 0x0211;

        public const int TagYCbCrSubsampling = 0x0212;

        public const int TagYCbCrPositioning = 0x0213;

        public const int TagReferenceBlackWhite = 0x0214;

        public const int TagStripRowCounts = 0x022F;

        public const int TagApplicationNotes = 0x02BC;

        public const int TagRelatedImageFileFormat = 0x1000;

        public const int TagRelatedImageWidth = 0x1001;

        public const int TagRelatedImageHeight = 0x1002;

        public const int TagRating = 0x4746;

        public const int TagCfaRepeatPatternDim = 0x828D;

        /// <summary>There are two definitions for CFA pattern, I don't know the difference...</summary>
        public const int TagCfaPattern2 = 0x828E;

        public const int TagBatteryLevel = 0x828F;

        public const int TagCopyright = 0x8298;

        /// <summary>Exposure time (reciprocal of shutter speed).</summary>
        /// <remarks>Unit is second.</remarks>
        public const int TagExposureTime = 0x829A;

        /// <summary>The actual F-number(F-stop) of lens when the image was taken.</summary>
        public const int TagFNumber = 0x829D;

        public const int TagIptcNaa = 0x83BB;

        public const int TagInterColorProfile = 0x8773;

        /// <summary>Exposure program that the camera used when image was taken.</summary>
        /// <remarks>
        /// '1' means
        /// manual control, '2' program normal, '3' aperture priority, '4' shutter
        /// priority, '5' program creative (slow program), '6' program action
        /// (high-speed program), '7' portrait mode, '8' landscape mode.
        /// </remarks>
        public const int TagExposureProgram = 0x8822;

        public const int TagSpectralSensitivity = 0x8824;

        public const int TagIsoEquivalent = 0x8827;

        /// <summary>Indicates the Opto-Electric Conversion Function (OECF) specified in ISO 14524.</summary>
        /// <remarks>
        /// OECF is the relationship between the camera optical input and the image values.
        /// <para />
        /// The values are:
        /// <list type="bullet">
        /// <item>Two shorts, indicating respectively number of columns, and number of rows.</item>
        /// <item>For each column, the column name in a null-terminated ASCII string.</item>
        /// <item>For each cell, an SRATIONAL value.</item>
        /// </list>
        /// </remarks>
        public const int TagOptoElectricConversionFunction = 0x8828;

        public const int TagInterlace = 0x8829;

        public const int TagTimeZoneOffsetTiffEp = 0x882A;

        public const int TagSelfTimerModeTiffEp = 0x882B;

        /// <summary>Applies to ISO tag.</summary>
        /// <remarks>
        /// 0 = Unknown
        /// 1 = Standard Output Sensitivity
        /// 2 = Recommended Exposure Index
        /// 3 = ISO Speed
        /// 4 = Standard Output Sensitivity and Recommended Exposure Index
        /// 5 = Standard Output Sensitivity and ISO Speed
        /// 6 = Recommended Exposure Index and ISO Speed
        /// 7 = Standard Output Sensitivity, Recommended Exposure Index and ISO Speed
        /// </remarks>
        public const int TagSensitivityType = 0x8830;

        public const int TagStandardOutputSensitivity = 0x8831;

        public const int TagRecommendedExposureIndex = 0x8832;

        /// <summary>Non-standard, but in use.</summary>
        public const int TagTimeZoneOffset = 0x882A;

        public const int TagSelfTimerMode = 0x882B;

        public const int TagExifVersion = 0x9000;

        public const int TagDateTimeOriginal = 0x9003;

        public const int TagDateTimeDigitized = 0x9004;

        public const int TagComponentsConfiguration = 0x9101;

        /// <summary>Average (rough estimate) compression level in JPEG bits per pixel.</summary>
        public const int TagCompressedAverageBitsPerPixel = 0x9102;

        /// <summary>Shutter speed by APEX value.</summary>
        /// <remarks>
        /// To convert this value to ordinary 'Shutter Speed';
        /// calculate this value's power of 2, then reciprocal. For example, if the
        /// ShutterSpeedValue is '4', shutter speed is 1/(24)=1/16 second.
        /// </remarks>
        public const int TagShutterSpeed = 0x9201;

        /// <summary>The actual aperture value of lens when the image was taken.</summary>
        /// <remarks>
        /// Unit is APEX.
        /// To convert this value to ordinary F-number (F-stop), calculate this value's
        /// power of root 2 (=1.4142). For example, if the ApertureValue is '5',
        /// F-number is 1.4142^5 = F5.6.
        /// </remarks>
        public const int TagAperture = 0x9202;

        public const int TagBrightnessValue = 0x9203;

        public const int TagExposureBias = 0x9204;

        /// <summary>Maximum aperture value of lens.</summary>
        /// <remarks>
        /// You can convert to F-number by calculating
        /// power of root 2 (same process of ApertureValue:0x9202).
        /// The actual aperture value of lens when the image was taken. To convert this
        /// value to ordinary f-number(f-stop), calculate the value's power of root 2
        /// (=1.4142). For example, if the ApertureValue is '5', f-number is 1.41425^5 = F5.6.
        /// </remarks>
        public const int TagMaxAperture = 0x9205;

        /// <summary>Indicates the distance the autofocus camera is focused to.</summary>
        /// <remarks>Tends to be less accurate as distance increases.</remarks>
        public const int TagSubjectDistance = 0x9206;

        /// <summary>Exposure metering method.</summary>
        /// <remarks>
        /// '0' means unknown, '1' average, '2' center weighted average,
        /// '3' spot, '4' multi-spot, '5' multi-segment, '6' partial,
        /// '255' other.
        /// </remarks>
        public const int TagMeteringMode = 0x9207;

        /// <summary>White balance (aka light source).</summary>
        /// <remarks>
        /// '0' means unknown, '1' daylight,
        /// '2' fluorescent, '3' tungsten, '10' flash, '17' standard light A,
        /// '18' standard light B, '19' standard light C, '20' D55, '21' D65,
        /// '22' D75, '255' other.
        /// </remarks>
        public const int TagWhiteBalance = 0x9208;

        /// <summary>
        /// 0x0  = 0000000 = No Flash
        /// 0x1  = 0000001 = Fired
        /// 0x5  = 0000101 = Fired, Return not detected
        /// 0x7  = 0000111 = Fired, Return detected
        /// 0x9  = 0001001 = On
        /// 0xd  = 0001101 = On, Return not detected
        /// 0xf  = 0001111 = On, Return detected
        /// 0x10 = 0010000 = Off
        /// 0x18 = 0011000 = Auto, Did not fire
        /// 0x19 = 0011001 = Auto, Fired
        /// 0x1d = 0011101 = Auto, Fired, Return not detected
        /// 0x1f = 0011111 = Auto, Fired, Return detected
        /// 0x20 = 0100000 = No flash function
        /// 0x41 = 1000001 = Fired, Red-eye reduction
        /// 0x45 = 1000101 = Fired, Red-eye reduction, Return not detected
        /// 0x47 = 1000111 = Fired, Red-eye reduction, Return detected
        /// 0x49 = 1001001 = On, Red-eye reduction
        /// 0x4d = 1001101 = On, Red-eye reduction, Return not detected
        /// 0x4f = 1001111 = On, Red-eye reduction, Return detected
        /// 0x59 = 1011001 = Auto, Fired, Red-eye reduction
        /// 0x5d = 1011101 = Auto, Fired, Red-eye reduction, Return not detected
        /// 0x5f = 1011111 = Auto, Fired, Red-eye reduction, Return detected
        ///        6543210 (positions)
        /// This is a bitmask.
        /// 0 = flash fired
        /// 1 = return detected
        /// 2 = return able to be detected
        /// 3 = unknown
        /// 4 = auto used
        /// 5 = unknown
        /// 6 = red eye reduction used
        /// </summary>
        public const int TagFlash = 0x9209;

        /// <summary>Focal length of lens used to take image.</summary>
        /// <remarks>
        /// Unit is millimeter.
        /// Nice digital cameras actually save the focal length as a function of how far they are zoomed in.
        /// </remarks>
        public const int TagFocalLength = 0x920A;

        public const int TagFlashEnergyTiffEp = 0x920B;

        public const int TagSpatialFreqResponseTiffEp = 0x920C;

        public const int TagNoise = 0x920D;

        public const int TagFocalPlaneXResolutionTiffEp = 0x920E;

        public const int TagFocalPlaneYResolutionTiffEp = 0x920F;

        public const int TagImageNumber = 0x9211;

        public const int TagSecurityClassification = 0x9212;

        public const int TagImageHistory = 0x9213;

        public const int TagSubjectLocationTiffEp = 0x9214;

        public const int TagExposureIndexTiffEp = 0x9215;

        public const int TagStandardIdTiffEp = 0x9216;

        /// <summary>This tag holds the Exif Makernote.</summary>
        /// <remarks>
        /// Makernotes are free to be in any format, though they are often IFDs.
        /// To determine the format, we consider the starting bytes of the makernote itself and sometimes the
        /// camera model and make.
        /// <para />
        /// The component count for this tag includes all of the bytes needed for the makernote.
        /// </remarks>
        public const int TagMakernote = 0x927C;

        public const int TagUserComment = 0x9286;

        public const int TagSubsecondTime = 0x9290;

        public const int TagSubsecondTimeOriginal = 0x9291;

        public const int TagSubsecondTimeDigitized = 0x9292;

        /// <summary>The image title, as used by Windows XP.</summary>
        public const int TagWinTitle = 0x9C9B;

        /// <summary>The image comment, as used by Windows XP.</summary>
        public const int TagWinComment = 0x9C9C;

        /// <summary>The image author, as used by Windows XP (called Artist in the Windows shell).</summary>
        public const int TagWinAuthor = 0x9C9D;

        /// <summary>The image keywords, as used by Windows XP.</summary>
        public const int TagWinKeywords = 0x9C9E;

        /// <summary>The image subject, as used by Windows XP.</summary>
        public const int TagWinSubject = 0x9C9F;

        public const int TagFlashpixVersion = 0xA000;

        /// <summary>Defines Color Space.</summary>
        /// <remarks>
        /// DCF image must use sRGB color space so value is
        /// always '1'. If the picture uses the other color space, value is
        /// '65535':Uncalibrated.
        /// </remarks>
        public const int TagColorSpace = 0xA001;

        public const int TagExifImageWidth = 0xA002;

        public const int TagExifImageHeight = 0xA003;

        public const int TagRelatedSoundFile = 0xA004;

        public const int TagFlashEnergy = 0xA20B;

        public const int TagSpatialFreqResponse = 0xA20C;

        public const int TagFocalPlaneXResolution = 0xA20E;

        public const int TagFocalPlaneYResolution = 0xA20F;

        /// <summary>Unit of FocalPlaneXResolution/FocalPlaneYResolution.</summary>
        /// <remarks>
        /// '1' means no-unit, '2' inch, '3' centimeter.
        /// Note: Some of Fujifilm's digicam(e.g.FX2700,FX2900,Finepix4700Z/40i etc)
        /// uses value '3' so it must be 'centimeter', but it seems that they use a
        /// '8.3mm?'(1/3in.?) to their ResolutionUnit. Fuji's BUG? Finepix4900Z has
        /// been changed to use value '2' but it doesn't match to actual value also.
        /// </remarks>
        public const int TagFocalPlaneResolutionUnit = 0xA210;

        public const int TagSubjectLocation = 0xA214;

        public const int TagExposureIndex = 0xA215;

        public const int TagSensingMethod = 0xA217;

        public const int TagFileSource = 0xA300;

        public const int TagSceneType = 0xA301;

        public const int TagCfaPattern = 0xA302;

        /// <summary>
        /// This tag indicates the use of special processing on image data, such as rendering
        /// geared to output.
        /// </summary>
        /// <remarks>
        /// When special processing is performed, the reader is expected to
        /// disable or minimize any further processing.
        /// Tag = 41985 (A401.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = 0
        /// 0 = Normal process
        /// 1 = Custom process
        /// Other = reserved
        /// </remarks>
        public const int TagCustomRendered = 0xA401;

        /// <summary>This tag indicates the exposure mode set when the image was shot.</summary>
        /// <remarks>
        /// In auto-bracketing mode, the camera shoots a series of frames of the
        /// same scene at different exposure settings.
        /// Tag = 41986 (A402.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = none
        /// 0 = Auto exposure
        /// 1 = Manual exposure
        /// 2 = Auto bracket
        /// Other = reserved
        /// </remarks>
        public const int TagExposureMode = 0xA402;

        /// <summary>This tag indicates the white balance mode set when the image was shot.</summary>
        /// <remarks>
        /// Tag = 41987 (A403.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = none
        /// 0 = Auto white balance
        /// 1 = Manual white balance
        /// Other = reserved
        /// </remarks>
        public const int TagWhiteBalanceMode = 0xA403;

        /// <summary>This tag indicates the digital zoom ratio when the image was shot.</summary>
        /// <remarks>
        /// If the numerator of the recorded value is 0, this indicates that digital zoom was
        /// not used.
        /// Tag = 41988 (A404.H)
        /// Type = RATIONAL
        /// Count = 1
        /// Default = none
        /// </remarks>
        public const int TagDigitalZoomRatio = 0xA404;

        /// <summary>
        /// This tag indicates the equivalent focal length assuming a 35mm film camera, in mm.
        /// </summary>
        /// <remarks>
        /// A value of 0 means the focal length is unknown. Note that this tag
        /// differs from the FocalLength tag.
        /// Tag = 41989 (A405.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = none
        /// </remarks>
        public const int Tag35MMFilmEquivFocalLength = 0xA405;

        /// <summary>This tag indicates the type of scene that was shot.</summary>
        /// <remarks>
        /// It can also be used to
        /// record the mode in which the image was shot. Note that this differs from
        /// the scene type (SceneType) tag.
        /// Tag = 41990 (A406.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = 0
        /// 0 = Standard
        /// 1 = Landscape
        /// 2 = Portrait
        /// 3 = Night scene
        /// Other = reserved
        /// </remarks>
        public const int TagSceneCaptureType = 0xA406;

        /// <summary>This tag indicates the degree of overall image gain adjustment.</summary>
        /// <remarks>
        /// Tag = 41991 (A407.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = none
        /// 0 = None
        /// 1 = Low gain up
        /// 2 = High gain up
        /// 3 = Low gain down
        /// 4 = High gain down
        /// Other = reserved
        /// </remarks>
        public const int TagGainControl = 0xA407;

        /// <summary>
        /// This tag indicates the direction of contrast processing applied by the camera
        /// when the image was shot.
        /// </summary>
        /// <remarks>
        /// Tag = 41992 (A408.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = 0
        /// 0 = Normal
        /// 1 = Soft
        /// 2 = Hard
        /// Other = reserved
        /// </remarks>
        public const int TagContrast = 0xA408;

        /// <summary>
        /// This tag indicates the direction of saturation processing applied by the camera
        /// when the image was shot.
        /// </summary>
        /// <remarks>
        /// Tag = 41993 (A409.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = 0
        /// 0 = Normal
        /// 1 = Low saturation
        /// 2 = High saturation
        /// Other = reserved
        /// </remarks>
        public const int TagSaturation = 0xA409;

        /// <summary>
        /// This tag indicates the direction of sharpness processing applied by the camera
        /// when the image was shot.
        /// </summary>
        /// <remarks>
        /// Tag = 41994 (A40A.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = 0
        /// 0 = Normal
        /// 1 = Soft
        /// 2 = Hard
        /// Other = reserved
        /// </remarks>
        public const int TagSharpness = 0xA40A;

        /// <summary>
        /// This tag indicates information on the picture-taking conditions of a particular
        /// camera model.
        /// </summary>
        /// <remarks>
        /// The tag is used only to indicate the picture-taking conditions in the reader.
        /// Tag = 41995 (A40B.H)
        /// Type = UNDEFINED
        /// Count = Any
        /// Default = none
        /// The information is recorded in the format shown below. The data is recorded
        /// in Unicode using SHORT type for the number of display rows and columns and
        /// UNDEFINED type for the camera settings. The Unicode (UCS-2) string including
        /// Signature is NULL terminated. The specifics of the Unicode string are as given
        /// in ISO/IEC 10464-1.
        /// Length  Type        Meaning
        /// ------+-----------+------------------
        /// 2       SHORT       Display columns
        /// 2       SHORT       Display rows
        /// Any     UNDEFINED   Camera setting-1
        /// Any     UNDEFINED   Camera setting-2
        /// :       :           :
        /// Any     UNDEFINED   Camera setting-n
        /// </remarks>
        public const int TagDeviceSettingDescription = 0xA40B;

        /// <summary>This tag indicates the distance to the subject.</summary>
        /// <remarks>
        /// Tag = 41996 (A40C.H)
        /// Type = SHORT
        /// Count = 1
        /// Default = none
        /// 0 = unknown
        /// 1 = Macro
        /// 2 = Close view
        /// 3 = Distant view
        /// Other = reserved
        /// </remarks>
        public const int TagSubjectDistanceRange = 0xA40C;

        /// <summary>This tag indicates an identifier assigned uniquely to each image.</summary>
        /// <remarks>
        /// It is recorded as an ASCII string equivalent to hexadecimal notation and 128-bit
        /// fixed length.
        /// Tag = 42016 (A420.H)
        /// Type = ASCII
        /// Count = 33
        /// Default = none
        /// </remarks>
        public const int TagImageUniqueId = 0xA420;

        /// <summary>String.</summary>
        public const int TagCameraOwnerName = 0xA430;

        /// <summary>String.</summary>
        public const int TagBodySerialNumber = 0xA431;

        /// <summary>An array of four Rational64u numbers giving focal and aperture ranges.</summary>
        public const int TagLensSpecification = 0xA432;

        /// <summary>String.</summary>
        public const int TagLensMake = 0xA433;

        /// <summary>String.</summary>
        public const int TagLensModel = 0xA434;

        /// <summary>String.</summary>
        public const int TagLensSerialNumber = 0xA435;

        /// <summary>Rational64u.</summary>
        public const int TagGamma = 0xA500;

        public const int TagPrintImageMatchingInfo = 0xC4A5;

        public const int TagPanasonicTitle = 0xC6D2;

        public const int TagPanasonicTitle2 = 0xC6D3;

        public const int TagPadding = 0xEA1C;

        public const int TagLens = 0xFDEA;

        protected static void AddExifTagNames([NotNull] Dictionary<int, string> map)
        {
            map[TagInteropIndex] = "Interoperability Index";
            map[TagInteropVersion] = "Interoperability Version";
            map[TagNewSubfileType] = "New Subfile Type";
            map[TagSubfileType] = "Subfile Type";
            map[TagImageWidth] = "Image Width";
            map[TagImageHeight] = "Image Height";
            map[TagBitsPerSample] = "Bits Per Sample";
            map[TagCompression] = "Compression";
            map[TagPhotometricInterpretation] = "Photometric Interpretation";
            map[TagThresholding] = "Thresholding";
            map[TagFillOrder] = "Fill Order";
            map[TagDocumentName] = "Document Name";
            map[TagImageDescription] = "Image Description";
            map[TagMake] = "Make";
            map[TagModel] = "Model";
            map[TagStripOffsets] = "Strip Offsets";
            map[TagOrientation] = "Orientation";
            map[TagSamplesPerPixel] = "Samples Per Pixel";
            map[TagRowsPerStrip] = "Rows Per Strip";
            map[TagStripByteCounts] = "Strip Byte Counts";
            map[TagMinSampleValue] = "Minimum Sample Value";
            map[TagMaxSampleValue] = "Maximum Sample Value";
            map[TagXResolution] = "X Resolution";
            map[TagYResolution] = "Y Resolution";
            map[TagPlanarConfiguration] = "Planar Configuration";
            map[TagPageName] = "Page Name";
            map[TagResolutionUnit] = "Resolution Unit";
            map[TagPageNumber] = "Page Number";
            map[TagTransferFunction] = "Transfer Function";
            map[TagSoftware] = "Software";
            map[TagDateTime] = "Date/Time";
            map[TagArtist] = "Artist";
            map[TagPredictor] = "Predictor";
            map[TagHostComputer] = "Host Computer";
            map[TagWhitePoint] = "White Point";
            map[TagPrimaryChromaticities] = "Primary Chromaticities";
            map[TagTileWidth] = "Tile Width";
            map[TagTileLength] = "Tile Length";
            map[TagTileOffsets] = "Tile Offsets";
            map[TagTileByteCounts] = "Tile Byte Counts";
            map[TagSubIfdOffset] = "Sub IFD Pointer(s)";
            map[TagTransferRange] = "Transfer Range";
            map[TagJpegTables] = "JPEG Tables";
            map[TagJpegProc] = "JPEG Proc";

            map[TagJpegRestartInterval] = "JPEG Restart Interval";
            map[TagJpegLosslessPredictors] = "JPEG Lossless Predictors";
            map[TagJpegPointTransforms] = "JPEG Point Transforms";
            map[TagJpegQTables] = "JPEGQ Tables";
            map[TagJpegDcTables] = "JPEGDC Tables";
            map[TagJpegAcTables] = "JPEGAC Tables";

            map[TagYCbCrCoefficients] = "YCbCr Coefficients";
            map[TagYCbCrSubsampling] = "YCbCr Sub-Sampling";
            map[TagYCbCrPositioning] = "YCbCr Positioning";
            map[TagReferenceBlackWhite] = "Reference Black/White";
            map[TagStripRowCounts] = "Strip Row Counts";
            map[TagApplicationNotes] = "Application Notes";
            map[TagRelatedImageFileFormat] = "Related Image File Format";
            map[TagRelatedImageWidth] = "Related Image Width";
            map[TagRelatedImageHeight] = "Related Image Height";
            map[TagRating] = "Rating";
            map[TagCfaRepeatPatternDim] = "CFA Repeat Pattern Dim";
            map[TagCfaPattern2] = "CFA Pattern";
            map[TagBatteryLevel] = "Battery Level";
            map[TagCopyright] = "Copyright";
            map[TagExposureTime] = "Exposure Time";
            map[TagFNumber] = "F-Number";
            map[TagIptcNaa] = "IPTC/NAA";
            map[TagInterColorProfile] = "Inter Color Profile";
            map[TagExposureProgram] = "Exposure Program";
            map[TagSpectralSensitivity] = "Spectral Sensitivity";
            map[TagIsoEquivalent] = "ISO Speed Ratings";
            map[TagOptoElectricConversionFunction] = "Opto-electric Conversion Function (OECF)";
            map[TagInterlace] = "Interlace";
            map[TagTimeZoneOffsetTiffEp] = "Time Zone Offset";
            map[TagSelfTimerModeTiffEp] = "Self Timer Mode";
            map[TagSensitivityType] = "Sensitivity Type";
            map[TagStandardOutputSensitivity] = "Standard Output Sensitivity";
            map[TagRecommendedExposureIndex] = "Recommended Exposure Index";
            map[TagTimeZoneOffset] = "Time Zone Offset";
            map[TagSelfTimerMode] = "Self Timer Mode";
            map[TagExifVersion] = "Exif Version";
            map[TagDateTimeOriginal] = "Date/Time Original";
            map[TagDateTimeDigitized] = "Date/Time Digitized";
            map[TagComponentsConfiguration] = "Components Configuration";
            map[TagCompressedAverageBitsPerPixel] = "Compressed Bits Per Pixel";
            map[TagShutterSpeed] = "Shutter Speed Value";
            map[TagAperture] = "Aperture Value";
            map[TagBrightnessValue] = "Brightness Value";
            map[TagExposureBias] = "Exposure Bias Value";
            map[TagMaxAperture] = "Max Aperture Value";
            map[TagSubjectDistance] = "Subject Distance";
            map[TagMeteringMode] = "Metering Mode";
            map[TagWhiteBalance] = "White Balance";
            map[TagFlash] = "Flash";
            map[TagFocalLength] = "Focal Length";
            map[TagFlashEnergyTiffEp] = "Flash Energy";
            map[TagSpatialFreqResponseTiffEp] = "Spatial Frequency Response";
            map[TagNoise] = "Noise";
            map[TagFocalPlaneXResolutionTiffEp] = "Focal Plane X Resolution";
            map[TagFocalPlaneYResolutionTiffEp] = "Focal Plane Y Resolution";
            map[TagImageNumber] = "Image Number";
            map[TagSecurityClassification] = "Security Classification";
            map[TagImageHistory] = "Image History";
            map[TagSubjectLocationTiffEp] = "Subject Location";
            map[TagExposureIndexTiffEp] = "Exposure Index";
            map[TagStandardIdTiffEp] = "TIFF/EP Standard ID";
            map[TagMakernote] = "Makernote";
            map[TagUserComment] = "User Comment";
            map[TagSubsecondTime] = "Sub-Sec Time";
            map[TagSubsecondTimeOriginal] = "Sub-Sec Time Original";
            map[TagSubsecondTimeDigitized] = "Sub-Sec Time Digitized";
            map[TagWinTitle] = "Windows XP Title";
            map[TagWinComment] = "Windows XP Comment";
            map[TagWinAuthor] = "Windows XP Author";
            map[TagWinKeywords] = "Windows XP Keywords";
            map[TagWinSubject] = "Windows XP Subject";
            map[TagFlashpixVersion] = "FlashPix Version";
            map[TagColorSpace] = "Color Space";
            map[TagExifImageWidth] = "Exif Image Width";
            map[TagExifImageHeight] = "Exif Image Height";
            map[TagRelatedSoundFile] = "Related Sound File";
            map[TagFlashEnergy] = "Flash Energy";
            map[TagSpatialFreqResponse] = "Spatial Frequency Response";
            map[TagFocalPlaneXResolution] = "Focal Plane X Resolution";
            map[TagFocalPlaneYResolution] = "Focal Plane Y Resolution";
            map[TagFocalPlaneResolutionUnit] = "Focal Plane Resolution Unit";
            map[TagSubjectLocation] = "Subject Location";
            map[TagExposureIndex] = "Exposure Index";
            map[TagSensingMethod] = "Sensing Method";
            map[TagFileSource] = "File Source";
            map[TagSceneType] = "Scene Type";
            map[TagCfaPattern] = "CFA Pattern";
            map[TagCustomRendered] = "Custom Rendered";
            map[TagExposureMode] = "Exposure Mode";
            map[TagWhiteBalanceMode] = "White Balance Mode";
            map[TagDigitalZoomRatio] = "Digital Zoom Ratio";
            map[Tag35MMFilmEquivFocalLength] = "Focal Length 35";
            map[TagSceneCaptureType] = "Scene Capture Type";
            map[TagGainControl] = "Gain Control";
            map[TagContrast] = "Contrast";
            map[TagSaturation] = "Saturation";
            map[TagSharpness] = "Sharpness";
            map[TagDeviceSettingDescription] = "Device Setting Description";
            map[TagSubjectDistanceRange] = "Subject Distance Range";
            map[TagImageUniqueId] = "Unique Image ID";
            map[TagCameraOwnerName] = "Camera Owner Name";
            map[TagBodySerialNumber] = "Body Serial Number";
            map[TagLensSpecification] = "Lens Specification";
            map[TagLensMake] = "Lens Make";
            map[TagLensModel] = "Lens Model";
            map[TagLensSerialNumber] = "Lens Serial Number";
            map[TagGamma] = "Gamma";
            map[TagPrintImageMatchingInfo] = "Print Image Matching (PIM) Info";
            map[TagPanasonicTitle] = "Panasonic Title";
            map[TagPanasonicTitle2] = "Panasonic Title (2)";
            map[TagPadding] = "Padding";
            map[TagLens] = "Lens";
        }
    }

    /// <summary>
    /// Provides human-readable string representations of tag values stored in a <see cref="ExifIfd0Directory"/>.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ExifIfd0Descriptor : ExifDescriptorBase<ExifIfd0Directory>
    {
        public ExifIfd0Descriptor([NotNull] ExifIfd0Directory directory)
            : base(directory)
        {
        }
    }

    /// <summary>Describes Exif tags from the IFD0 directory.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ExifIfd0Directory : ExifDirectoryBase
    {
        /// <summary>This tag is a pointer to the Exif SubIFD.</summary>
        public const int TagExifSubIfdOffset = 0x8769;

        /// <summary>This tag is a pointer to the Exif GPS IFD.</summary>
        public const int TagGpsInfoOffset = 0x8825;

        public ExifIfd0Directory()
        {
            SetDescriptor(new ExifIfd0Descriptor(this));
        }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>();

        static ExifIfd0Directory()
        {
            AddExifTagNames(_tagNameMap);
        }

        public override string Name { get { return "Exif IFD0"; } }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    /// <summary>
    /// Provides human-readable string representations of tag values stored in a <see cref="GpsDirectory"/>.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class GpsDescriptor : TagDescriptor<GpsDirectory>
    {
        public GpsDescriptor([NotNull] GpsDirectory directory)
            : base(directory)
        {
        }

        public override string GetDescription(int tagType)
        {
            switch (tagType)
            {
                case GpsDirectory.TagVersionId:
                    return GetGpsVersionIdDescription();
                case GpsDirectory.TagAltitude:
                    return GetGpsAltitudeDescription();
                case GpsDirectory.TagAltitudeRef:
                    return GetGpsAltitudeRefDescription();
                case GpsDirectory.TagStatus:
                    return GetGpsStatusDescription();
                case GpsDirectory.TagMeasureMode:
                    return GetGpsMeasureModeDescription();
                case GpsDirectory.TagSpeedRef:
                    return GetGpsSpeedRefDescription();
                case GpsDirectory.TagTrackRef:
                case GpsDirectory.TagImgDirectionRef:
                case GpsDirectory.TagDestBearingRef:
                    return GetGpsDirectionReferenceDescription(tagType);
                case GpsDirectory.TagTrack:
                case GpsDirectory.TagImgDirection:
                case GpsDirectory.TagDestBearing:
                    return GetGpsDirectionDescription(tagType);
                case GpsDirectory.TagDestDistanceRef:
                    return GetGpsDestinationReferenceDescription();
                case GpsDirectory.TagTimeStamp:
                    return GetGpsTimeStampDescription();
                case GpsDirectory.TagLongitude:
                    // three rational numbers -- displayed in HH"MM"SS.ss
                    return GetGpsLongitudeDescription();
                case GpsDirectory.TagLatitude:
                    // three rational numbers -- displayed in HH"MM"SS.ss
                    return GetGpsLatitudeDescription();
                case GpsDirectory.TagDifferential:
                    return GetGpsDifferentialDescription();
                default:
                    return base.GetDescription(tagType);
            }
        }

        [CanBeNull]
        private string GetGpsVersionIdDescription()
        {
            return GetVersionBytesDescription(GpsDirectory.TagVersionId, 1);
        }

        [CanBeNull]
        public string GetGpsLatitudeDescription()
        {
            var location = Directory.GetGeoLocation();
            return location == null ? null : GeoLocation.DecimalToDegreesMinutesSecondsString(location.Latitude);
        }

        [CanBeNull]
        public string GetGpsLongitudeDescription()
        {
            var location = Directory.GetGeoLocation();
            return location == null ? null : GeoLocation.DecimalToDegreesMinutesSecondsString(location.Longitude);
        }

        [CanBeNull]
        public string GetGpsTimeStampDescription()
        {
            // time in hour, min, sec
            var timeComponents = Directory.GetRationalArray(GpsDirectory.TagTimeStamp);
            return timeComponents == null
                ? null
                : string.Format("{0:D2}:{1:D2}:{2:00.000} UTC",timeComponents[0].ToInt32(), timeComponents[1].ToInt32(), timeComponents[2].ToDouble());
        }

        [CanBeNull]
        public string GetGpsDestinationReferenceDescription()
        {
            var value = Directory.GetString(GpsDirectory.TagDestDistanceRef);
            if (value == null)
                return null;

            switch (value.Trim().ToUpper())
            {
                case "K":
                    return "kilometers";
                case "M":
                    return "miles";
                case "N":
                    return "knots";
            }

            return "Unknown (" + value.Trim() + ")";
        }

        [CanBeNull]
        public string GetGpsDirectionDescription(int tagType)
        {
            Rational angle;
            if (!Directory.TryGetRational(tagType, out angle))
                return null;
            // provide a decimal version of rational numbers in the description, to avoid strings like "35334/199 degrees"
            return angle.ToDouble().ToString("0.##") + " degrees";
        }

        [CanBeNull]
        public string GetGpsDirectionReferenceDescription(int tagType)
        {
            var value = Directory.GetString(tagType);
            if (value == null)
                return null;

            switch (value.Trim().ToUpper())
            {
                case "T":
                    return "True direction";
                case "M":
                    return "Magnetic direction";
            }

            return "Unknown (" + value.Trim() + ")";
        }

        [CanBeNull]
        public string GetGpsSpeedRefDescription()
        {
            var value = Directory.GetString(GpsDirectory.TagSpeedRef);
            if (value == null)
                return null;

            switch (value.Trim().ToUpper())
            {
                case "K":
                    return "kph";
                case "M":
                    return "mph";
                case "N":
                    return "knots";
            }

            return "Unknown (" + value.Trim() + ")";
        }

        [CanBeNull]
        public string GetGpsMeasureModeDescription()
        {
            var value = Directory.GetString(GpsDirectory.TagMeasureMode);
            if (value == null)
                return null;

            switch (value.Trim())
            {
                case "2":
                    return "2-dimensional measurement";
                case "3":
                    return "3-dimensional measurement";
            }
            return "Unknown (" + value.Trim() + ")";
        }


        [CanBeNull]
        public string GetGpsStatusDescription()
        {
            var value = Directory.GetString(GpsDirectory.TagStatus);
            if (value == null)
                return null;

            switch (value.Trim().ToUpper())
            {
                case "A":
                    return "Active (Measurement in progress)";
                case "V":
                    return "Void (Measurement Interoperability)";
            }

            return "Unknown (" + value.Trim() + ")";
        }

        [CanBeNull]
        public string GetGpsAltitudeRefDescription()
        {
            return GetIndexedDescription(GpsDirectory.TagAltitudeRef,
                "Sea level", "Below sea level");
        }

        [CanBeNull]
        public string GetGpsAltitudeDescription()
        {
            Rational value;
            if (!Directory.TryGetRational(GpsDirectory.TagAltitude, out value))
                return null;
            return value.ToInt32() + " metres";
        }

        [CanBeNull]
        public string GetGpsDifferentialDescription()
        {
            return GetIndexedDescription(GpsDirectory.TagDifferential,
                "No Correction", "Differential Corrected");
        }

        [CanBeNull]
        public string GetDegreesMinutesSecondsDescription()
        {
            var location = Directory.GetGeoLocation();
            return (location != null ? location.ToDmsString() : null);
        }
    }

    /// <summary>Describes Exif tags that contain Global Positioning System (GPS) data.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class GpsDirectory : ExifDirectoryBase
    {
        /// <summary>GPS tag version GPSVersionID 0 0 BYTE 4</summary>
        public const int TagVersionId = 0x0000;

        /// <summary>North or South Latitude GPSLatitudeRef 1 1 ASCII 2</summary>
        public const int TagLatitudeRef = 0x0001;

        /// <summary>Latitude GPSLatitude 2 2 RATIONAL 3</summary>
        public const int TagLatitude = 0x0002;

        /// <summary>East or West Longitude GPSLongitudeRef 3 3 ASCII 2</summary>
        public const int TagLongitudeRef = 0x0003;

        /// <summary>Longitude GPSLongitude 4 4 RATIONAL 3</summary>
        public const int TagLongitude = 0x0004;

        /// <summary>Altitude reference GPSAltitudeRef 5 5 BYTE 1</summary>
        public const int TagAltitudeRef = 0x0005;

        /// <summary>Altitude GPSAltitude 6 6 RATIONAL 1</summary>
        public const int TagAltitude = 0x0006;

        /// <summary>GPS time (atomic clock) GPSTimeStamp 7 7 RATIONAL 3</summary>
        public const int TagTimeStamp = 0x0007;

        /// <summary>GPS satellites used for measurement GPSSatellites 8 8 ASCII Any</summary>
        public const int TagSatellites = 0x0008;

        /// <summary>GPS receiver status GPSStatus 9 9 ASCII 2</summary>
        public const int TagStatus = 0x0009;

        /// <summary>GPS measurement mode GPSMeasureMode 10 A ASCII 2</summary>
        public const int TagMeasureMode = 0x000A;

        /// <summary>Measurement precision GPSDOP 11 B RATIONAL 1</summary>
        public const int TagDop = 0x000B;

        /// <summary>Speed unit GPSSpeedRef 12 C ASCII 2</summary>
        public const int TagSpeedRef = 0x000C;

        /// <summary>Speed of GPS receiver GPSSpeed 13 D RATIONAL 1</summary>
        public const int TagSpeed = 0x000D;

        /// <summary>Reference for direction of movement GPSTrackRef 14 E ASCII 2</summary>
        public const int TagTrackRef = 0x000E;

        /// <summary>Direction of movement GPSTrack 15 F RATIONAL 1</summary>
        public const int TagTrack = 0x000F;

        /// <summary>Reference for direction of image GPSImgDirectionRef 16 10 ASCII 2</summary>
        public const int TagImgDirectionRef = 0x0010;

        /// <summary>Direction of image GPSImgDirection 17 11 RATIONAL 1</summary>
        public const int TagImgDirection = 0x0011;

        /// <summary>Geodetic survey data used GPSMapDatum 18 12 ASCII Any</summary>
        public const int TagMapDatum = 0x0012;

        /// <summary>Reference for latitude of destination GPSDestLatitudeRef 19 13 ASCII 2</summary>
        public const int TagDestLatitudeRef = 0x0013;

        /// <summary>Latitude of destination GPSDestLatitude 20 14 RATIONAL 3</summary>
        public const int TagDestLatitude = 0x0014;

        /// <summary>Reference for longitude of destination GPSDestLongitudeRef 21 15 ASCII 2</summary>
        public const int TagDestLongitudeRef = 0x0015;

        /// <summary>Longitude of destination GPSDestLongitude 22 16 RATIONAL 3</summary>
        public const int TagDestLongitude = 0x0016;

        /// <summary>Reference for bearing of destination GPSDestBearingRef 23 17 ASCII 2</summary>
        public const int TagDestBearingRef = 0x0017;

        /// <summary>Bearing of destination GPSDestBearing 24 18 RATIONAL 1</summary>
        public const int TagDestBearing = 0x0018;

        /// <summary>Reference for distance to destination GPSDestDistanceRef 25 19 ASCII 2</summary>
        public const int TagDestDistanceRef = 0x0019;

        /// <summary>Distance to destination GPSDestDistance 26 1A RATIONAL 1</summary>
        public const int TagDestDistance = 0x001A;

        /// <summary>Values of "GPS", "CELLID", "WLAN" or "MANUAL" by the EXIF spec.</summary>
        public const int TagProcessingMethod = 0x001B;

        public const int TagAreaInformation = 0x001C;

        public const int TagDateStamp = 0x001D;

        public const int TagDifferential = 0x001E;

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>();

        static GpsDirectory()
        {
            AddExifTagNames(_tagNameMap);

            // NOTE there is overlap between the base Exif tags and the GPS tags,
            // so we add the GPS tags after to ensure they're kept.

            _tagNameMap[TagVersionId] = "GPS Version ID";
            _tagNameMap[TagLatitudeRef] = "GPS Latitude Ref";
            _tagNameMap[TagLatitude] = "GPS Latitude";
            _tagNameMap[TagLongitudeRef] = "GPS Longitude Ref";
            _tagNameMap[TagLongitude] = "GPS Longitude";
            _tagNameMap[TagAltitudeRef] = "GPS Altitude Ref";
            _tagNameMap[TagAltitude] = "GPS Altitude";
            _tagNameMap[TagTimeStamp] = "GPS Time-Stamp";
            _tagNameMap[TagSatellites] = "GPS Satellites";
            _tagNameMap[TagStatus] = "GPS Status";
            _tagNameMap[TagMeasureMode] = "GPS Measure Mode";
            _tagNameMap[TagDop] = "GPS DOP";
            _tagNameMap[TagSpeedRef] = "GPS Speed Ref";
            _tagNameMap[TagSpeed] = "GPS Speed";
            _tagNameMap[TagTrackRef] = "GPS Track Ref";
            _tagNameMap[TagTrack] = "GPS Track";
            _tagNameMap[TagImgDirectionRef] = "GPS Img Direction Ref";
            _tagNameMap[TagImgDirection] = "GPS Img Direction";
            _tagNameMap[TagMapDatum] = "GPS Map Datum";
            _tagNameMap[TagDestLatitudeRef] = "GPS Dest Latitude Ref";
            _tagNameMap[TagDestLatitude] = "GPS Dest Latitude";
            _tagNameMap[TagDestLongitudeRef] = "GPS Dest Longitude Ref";
            _tagNameMap[TagDestLongitude] = "GPS Dest Longitude";
            _tagNameMap[TagDestBearingRef] = "GPS Dest Bearing Ref";
            _tagNameMap[TagDestBearing] = "GPS Dest Bearing";
            _tagNameMap[TagDestDistanceRef] = "GPS Dest Distance Ref";
            _tagNameMap[TagDestDistance] = "GPS Dest Distance";
            _tagNameMap[TagProcessingMethod] = "GPS Processing Method";
            _tagNameMap[TagAreaInformation] = "GPS Area Information";
            _tagNameMap[TagDateStamp] = "GPS Date Stamp";
            _tagNameMap[TagDifferential] = "GPS Differential";
        }

        public GpsDirectory()
        {
            SetDescriptor(new GpsDescriptor(this));
        }

        public override string Name { get { return "GPS"; } }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }

        /// <summary>
        /// Parses various tags in an attempt to obtain a single object representing the latitude and longitude
        /// at which this image was captured.
        /// </summary>
        /// <returns>The geographical location of this image, if possible, otherwise <c>null</c>.</returns>
        [CanBeNull]
        public GeoLocation GetGeoLocation()
        {
            var latitudes = this.GetRationalArray(TagLatitude);
            var longitudes = this.GetRationalArray(TagLongitude);
            var latitudeRef = this.GetString(TagLatitudeRef);
            var longitudeRef = this.GetString(TagLongitudeRef);

            // Make sure we have the required values
            if (latitudes == null || latitudes.Length != 3)
                return null;
            if (longitudes == null || longitudes.Length != 3)
                return null;
            if (latitudeRef == null || longitudeRef == null)
                return null;

            var lat = GeoLocation.DegreesMinutesSecondsToDecimal(latitudes[0],  latitudes[1],  latitudes[2],  latitudeRef.Equals("S", StringComparison.OrdinalIgnoreCase));
            var lon = GeoLocation.DegreesMinutesSecondsToDecimal(longitudes[0], longitudes[1], longitudes[2], longitudeRef.Equals("W", StringComparison.OrdinalIgnoreCase));

            // This can return null, in cases where the conversion was not possible
            if (lat == null || lon == null)
                return null;

            return new GeoLocation((double)lat, (double)lon);
        }

        /// <summary>
        /// Parses values for <see cref="TagDateStamp"/> and <see cref="TagTimeStamp"/> to produce a single
        /// <see cref="DateTime"/> value representing when this image was captured according to the GPS unit.
        /// </summary>
        public bool TryGetGpsDate(out DateTime date)
        {
            if (!this.TryGetDateTime(TagDateStamp, out date))
                return false;

            var timeComponents = this.GetRationalArray(TagTimeStamp);

            if (timeComponents == null || timeComponents.Length != 3)
                return false;

            date = date
                .AddHours(timeComponents[0].ToDouble())
                .AddMinutes(timeComponents[1].ToDouble())
                .AddSeconds(timeComponents[2].ToDouble());

            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            return true;
        }
    }

    /// <summary>
    /// Provides human-readable string representations of tag values stored in a <see cref="ExifInteropDirectory"/>.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ExifInteropDescriptor : ExifDescriptorBase<ExifInteropDirectory>
    {
        public ExifInteropDescriptor([NotNull] ExifInteropDirectory directory)
            : base(directory)
        {
        }
    }

    /// <summary>
    /// Provides human-readable string representations of tag values stored in a <see cref="ExifThumbnailDirectory"/>.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class ExifThumbnailDescriptor : ExifDescriptorBase<ExifThumbnailDirectory>
    {
        public ExifThumbnailDescriptor([NotNull] ExifThumbnailDirectory directory)
            : base(directory)
        {
        }

        public override string GetDescription(int tagType)
        {
            switch (tagType)
            {
                case ExifThumbnailDirectory.TagThumbnailOffset:
                    return GetThumbnailOffsetDescription();
                case ExifThumbnailDirectory.TagThumbnailLength:
                    return GetThumbnailLengthDescription();
                default:
                    return base.GetDescription(tagType);
            }
        }

        [CanBeNull]
        public string GetThumbnailLengthDescription()
        {
            var value = Directory.GetString(ExifThumbnailDirectory.TagThumbnailLength);
            return value == null ? null : value + " bytes";
        }

        [CanBeNull]
        public string GetThumbnailOffsetDescription()
        {
            var value = Directory.GetString(ExifThumbnailDirectory.TagThumbnailOffset);
            return value == null ? null : value + " bytes";
        }
    }

    /// <summary>Describes Exif interoperability tags.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ExifInteropDirectory : ExifDirectoryBase
    {
        [NotNull] private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>();

        static ExifInteropDirectory()
        {
            AddExifTagNames(_tagNameMap);
        }

        public ExifInteropDirectory()
        {
            SetDescriptor(new ExifInteropDescriptor(this));
        }

        public override string Name { get { return "Interoperability"; } }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    /// <summary>One of several Exif directories.</summary>
    /// <remarks>Otherwise known as IFD1, this directory holds information about an embedded thumbnail image.</remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class ExifThumbnailDirectory : ExifDirectoryBase
    {
        /// <summary>The offset to thumbnail image bytes.</summary>
        public const int TagThumbnailOffset = 0x0201;

        /// <summary>The size of the thumbnail image data in bytes.</summary>
        public const int TagThumbnailLength = 0x0202;

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagThumbnailOffset, "Thumbnail Offset" },
            { TagThumbnailLength, "Thumbnail Length" }
        };

        static ExifThumbnailDirectory()
        {
            AddExifTagNames(_tagNameMap);
        }

        public ExifThumbnailDirectory()
        {
            SetDescriptor(new ExifThumbnailDescriptor(this));
        }

        public override string Name { get { return "Exif Thumbnail"; } }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    /// <summary>
    /// Provides human-readable string representations of tag values stored in a <see cref="ExifImageDirectory"/>.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class ExifImageDescriptor : ExifDescriptorBase<ExifImageDirectory>
    {
        public ExifImageDescriptor([NotNull] ExifImageDirectory directory)
            : base(directory)
        {
        }
    }

    /// <summary>One of several Exif directories.</summary>
    /// <remarks>Holds information about image IFD's in a chain after the first. The first page is stored in IFD0.</remarks>
    /// <remarks>Currently, this only applies to multi-page TIFF images</remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class ExifImageDirectory : ExifDirectoryBase
    {
        public ExifImageDirectory()
        {
            SetDescriptor(new ExifImageDescriptor(this));
        }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>();

        static ExifImageDirectory()
        {
            AddExifTagNames(_tagNameMap);
        }

        public override string Name { get { return "Exif Image"; } }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }
}

namespace MetadataExtractor.Formats.Exif
{
    using MetadataExtractor.Formats.Tiff;
    using MetadataExtractor.Formats.Jpeg;

    /// <summary>
    /// Implementation of <see cref="ITiffHandler"/> used for handling TIFF tags according to the Exif standard.
    /// <para />
    /// Includes support for camera manufacturer makernotes.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public class ExifTiffHandler : DirectoryTiffHandler
    {
        public ExifTiffHandler([NotNull] List<Directory> directories)
            : base(directories)
        {}

        /// <exception cref="TiffProcessingException"/>
        public override void SetTiffMarker(int marker)
        {
            const int standardTiffMarker     = 0x002A;
            const int olympusRawTiffMarker   = 0x4F52; // for ORF files
            const int olympusRawTiffMarker2  = 0x5352; // for ORF files
            const int panasonicRawTiffMarker = 0x0055; // for RAW, RW2, and RWL files

            switch (marker)
            {
                case standardTiffMarker:
                case olympusRawTiffMarker:  // Todo: implement an IFD0
                case olympusRawTiffMarker2: // Todo: implement an IFD0
                    PushDirectory(new ExifIfd0Directory());
                    break;
                case panasonicRawTiffMarker:
#if METADATAEXTRACTOR_HAVE_MAKER_NOTES
                    PushDirectory(new PanasonicRawIfd0Directory());
#endif
                    break;
                default:
                    throw new TiffProcessingException(string.Format("Unexpected TIFF marker: 0x{0:X}", marker));
            }
        }

        public override bool TryEnterSubIfd(int tagId)
        {
            if (tagId == ExifDirectoryBase.TagSubIfdOffset)
            {
                PushDirectory(new ExifSubIfdDirectory());
                return true;
            }

            if (CurrentDirectory is ExifIfd0Directory
#if METADATAEXTRACTOR_HAVE_MAKER_NOTES
                    || CurrentDirectory is PanasonicRawIfd0Directory
#endif
                )
            {
                if (tagId == ExifIfd0Directory.TagExifSubIfdOffset)
                {
                    PushDirectory(new ExifSubIfdDirectory());
                    return true;
                }
                if (tagId == ExifIfd0Directory.TagGpsInfoOffset)
                {
                    PushDirectory(new GpsDirectory());
                    return true;
                }
            }
            else if (CurrentDirectory is ExifSubIfdDirectory)
            {
                if (tagId == ExifSubIfdDirectory.TagInteropOffset)
                {
                    PushDirectory(new ExifInteropDirectory());
                    return true;
                }
            }
#if METADATAEXTRACTOR_HAVE_MAKER_NOTES
            else if (CurrentDirectory is OlympusMakernoteDirectory)
            {
                // Note: these also appear in CustomProcessTag because some are IFD pointers while others begin immediately
                // for the same directories
                switch (tagId)
                {
                    case OlympusMakernoteDirectory.TagEquipment:
                        PushDirectory(new OlympusEquipmentMakernoteDirectory());
                        return true;
                    case OlympusMakernoteDirectory.TagCameraSettings:
                        PushDirectory(new OlympusCameraSettingsMakernoteDirectory());
                        return true;
                    case OlympusMakernoteDirectory.TagRawDevelopment:
                        PushDirectory(new OlympusRawDevelopmentMakernoteDirectory());
                        return true;
                    case OlympusMakernoteDirectory.TagRawDevelopment2:
                        PushDirectory(new OlympusRawDevelopment2MakernoteDirectory());
                        return true;
                    case OlympusMakernoteDirectory.TagImageProcessing:
                        PushDirectory(new OlympusImageProcessingMakernoteDirectory());
                        return true;
                    case OlympusMakernoteDirectory.TagFocusInfo:
                        PushDirectory(new OlympusFocusInfoMakernoteDirectory());
                        return true;
                    case OlympusMakernoteDirectory.TagRawInfo:
                        PushDirectory(new OlympusRawInfoMakernoteDirectory());
                        return true;
                    case OlympusMakernoteDirectory.TagMainInfo:
                        PushDirectory(new OlympusMakernoteDirectory());
                        return true;
                }
            }
#endif

            return false;
        }

        public override bool HasFollowerIfd()
        {
            // If the next Ifd is IFD1, it's a thumbnail for JPG and some TIFF-based images
            // NOTE: this is not true for some other image types, but those are not implemented yet
            if (CurrentDirectory is ExifIfd0Directory)
            {
                PushDirectory(new ExifThumbnailDirectory());
                return true;
            }
            else
            {
                // In multipage TIFFs, the 'follower' IFD points to the next image in the set
                PushDirectory(new ExifImageDirectory());
                return true;
            }
        }

        public override bool CustomProcessTag(int tagOffset, ICollection<int> processedIfdOffsets, IndexedReader reader, int tagId, int byteCount)
        {
            // Some 0x0000 tags have a 0 byteCount. Determine whether it's bad.
            if (tagId == 0)
            {
                if (CurrentDirectory.ContainsTag(tagId))
                {
                    // Let it go through for now. Some directories handle it, some don't.
                    return false;
                }

                // Skip over 0x0000 tags that don't have any associated bytes. No idea what it contains in this case, if anything.
                if (byteCount == 0)
                    return true;
            }

            // Custom processing for the Makernote tag
            if (tagId == ExifDirectoryBase.TagMakernote && CurrentDirectory is ExifSubIfdDirectory)
                return ProcessMakernote(tagOffset, processedIfdOffsets, reader);

#if METADATAEXTRACTOR_HAVE_UNDATED_SUPPORT
            // Custom processing for embedded IPTC data
            if (tagId == ExifDirectoryBase.TagIptcNaa && CurrentDirectory is ExifIfd0Directory)
            {
                // NOTE Adobe sets type 4 for IPTC instead of 7
                if (reader.GetSByte(tagOffset) == 0x1c)
                {
                    var iptcBytes = reader.GetBytes(tagOffset, byteCount);
                    var iptcDirectory = new IptcReader().Extract(new SequentialByteArrayReader(iptcBytes), iptcBytes.Length);
                    iptcDirectory.Parent = CurrentDirectory;
                    Directories.Add(iptcDirectory);
                    return true;
                }
                return false;
            }
#endif

#if METADATAEXTRACTOR_HAVE_XMP_SUPPORT
            // Custom processing for embedded XMP data
            if (tagId == ExifDirectoryBase.TagApplicationNotes && CurrentDirectory is ExifIfd0Directory)
            {
                var xmpDirectory = new XmpReader().Extract(reader.GetNullTerminatedBytes(tagOffset, byteCount));
                xmpDirectory.Parent = CurrentDirectory;
                Directories.Add(xmpDirectory);
                return true;
            }
#endif

            if (HandlePrintIM(CurrentDirectory, tagId))
            {
                var printIMDirectory = new PrintIMDirectory { Parent = CurrentDirectory };
                Directories.Add(printIMDirectory);
                ProcessPrintIM(printIMDirectory, tagOffset, reader, byteCount);
                return true;
            }

#if METADATAEXTRACTOR_HAVE_MAKER_NOTES
            // Note: these also appear in TryEnterSubIfd because some are IFD pointers while others begin immediately
            // for the same directories
            if (CurrentDirectory is OlympusMakernoteDirectory)
            {
                switch (tagId)
                {
                    case OlympusMakernoteDirectory.TagEquipment:
                        PushDirectory(new OlympusEquipmentMakernoteDirectory());
                        TiffReader.ProcessIfd(this, reader, processedIfdOffsets, tagOffset);
                        return true;
                    case OlympusMakernoteDirectory.TagCameraSettings:
                        PushDirectory(new OlympusCameraSettingsMakernoteDirectory());
                        TiffReader.ProcessIfd(this, reader, processedIfdOffsets, tagOffset);
                        return true;
                    case OlympusMakernoteDirectory.TagRawDevelopment:
                        PushDirectory(new OlympusRawDevelopmentMakernoteDirectory());
                        TiffReader.ProcessIfd(this, reader, processedIfdOffsets, tagOffset);
                        return true;
                    case OlympusMakernoteDirectory.TagRawDevelopment2:
                        PushDirectory(new OlympusRawDevelopment2MakernoteDirectory());
                        TiffReader.ProcessIfd(this, reader, processedIfdOffsets, tagOffset);
                        return true;
                    case OlympusMakernoteDirectory.TagImageProcessing:
                        PushDirectory(new OlympusImageProcessingMakernoteDirectory());
                        TiffReader.ProcessIfd(this, reader, processedIfdOffsets, tagOffset);
                        return true;
                    case OlympusMakernoteDirectory.TagFocusInfo:
                        PushDirectory(new OlympusFocusInfoMakernoteDirectory());
                        TiffReader.ProcessIfd(this, reader, processedIfdOffsets, tagOffset);
                        return true;
                    case OlympusMakernoteDirectory.TagRawInfo:
                        PushDirectory(new OlympusRawInfoMakernoteDirectory());
                        TiffReader.ProcessIfd(this, reader, processedIfdOffsets, tagOffset);
                        return true;
                    case OlympusMakernoteDirectory.TagMainInfo:
                        PushDirectory(new OlympusMakernoteDirectory());
                        TiffReader.ProcessIfd(this, reader, processedIfdOffsets, tagOffset);
                        return true;
                }
            }

            if (CurrentDirectory is PanasonicRawIfd0Directory)
            {
                // these contain binary data with specific offsets, and can't be processed as regular ifd's.
                // The binary data is broken into 'fake' tags and there is a pattern.
                switch (tagId)
                {
                    case PanasonicRawIfd0Directory.TagWbInfo:
                        var dirWbInfo = new PanasonicRawWbInfoDirectory { Parent = CurrentDirectory };
                        Directories.Add(dirWbInfo);
                        ProcessBinary(dirWbInfo, tagOffset, reader, byteCount, false, 2);
                        return true;
                    case PanasonicRawIfd0Directory.TagWbInfo2:
                        var dirWbInfo2 = new PanasonicRawWbInfo2Directory { Parent = CurrentDirectory };
                        Directories.Add(dirWbInfo2);
                        ProcessBinary(dirWbInfo2, tagOffset, reader, byteCount, false, 3);
                        return true;
                    case PanasonicRawIfd0Directory.TagDistortionInfo:
                        var dirDistort = new PanasonicRawDistortionDirectory { Parent = CurrentDirectory };
                        Directories.Add(dirDistort);
                        ProcessBinary(dirDistort, tagOffset, reader, byteCount);
                        return true;
                }
            }

            // Panasonic RAW sometimes contains an embedded version of the data as a JPG file.
            if (tagId == PanasonicRawIfd0Directory.TagJpgFromRaw && CurrentDirectory is PanasonicRawIfd0Directory)
            {
                var jpegrawbytes = reader.GetBytes(tagOffset, byteCount);

                // Extract information from embedded image since it is metadata-rich
                var jpegmem = new MemoryStream(jpegrawbytes);
                var jpegDirectory = Jpeg.JpegMetadataReader.ReadMetadata(jpegmem);
                foreach (var dir in jpegDirectory)
                {
                    dir.Parent = CurrentDirectory;
                    Directories.Add(dir);
                }
                return true;
            }
#endif

            return false;
        }

        public override bool TryCustomProcessFormat(int tagId, TiffDataFormatCode formatCode, uint componentCount, out long byteCount)
        {
            if ((ushort)formatCode == 13u)
            {
                byteCount = 4L*componentCount;
                return true;
            }

            // an unknown (0) formatCode needs to be potentially handled later as a highly custom directory tag
            if (formatCode == 0)
            {
                byteCount = 0;
                return true;
            }

            byteCount = default(int);
            return false;
        }

        /// <exception cref="System.IO.IOException"/>
        private bool ProcessMakernote(int makernoteOffset, [NotNull] ICollection<int> processedIfdOffsets, [NotNull] IndexedReader reader)
        {
            Debug.Assert(makernoteOffset >= 0, "makernoteOffset >= 0");

#if METADATAEXTRACTOR_HAVE_MAKER_NOTES
            Directory exifIfd0Directory = Directories.Find((Directory a) => { return a is ExifIfd0Directory; });
            var cameraMake = (exifIfd0Directory != null ? exifIfd0Directory.GetString(ExifDirectoryBase.TagMake) : null);

            var firstTwoChars    = reader.GetString(makernoteOffset, 2, Encoding.UTF8);
            var firstThreeChars  = reader.GetString(makernoteOffset, 3, Encoding.UTF8);
            var firstFourChars   = reader.GetString(makernoteOffset, 4, Encoding.UTF8);
            var firstFiveChars   = reader.GetString(makernoteOffset, 5, Encoding.UTF8);
            var firstSixChars    = reader.GetString(makernoteOffset, 6, Encoding.UTF8);
            var firstSevenChars  = reader.GetString(makernoteOffset, 7, Encoding.UTF8);
            var firstEightChars  = reader.GetString(makernoteOffset, 8, Encoding.UTF8);
            var firstNineChars   = reader.GetString(makernoteOffset, 9, Encoding.UTF8);
            var firstTenChars    = reader.GetString(makernoteOffset, 10, Encoding.UTF8);
            var firstTwelveChars = reader.GetString(makernoteOffset, 12, Encoding.UTF8);

            if (string.Equals("OLYMP\0", firstSixChars, StringComparison.Ordinal) ||
                string.Equals("EPSON", firstFiveChars, StringComparison.Ordinal) ||
                string.Equals("AGFA", firstFourChars, StringComparison.Ordinal))
            {
                // Olympus Makernote
                // Epson and Agfa use Olympus makernote standard: http://www.ozhiker.com/electronics/pjmt/jpeg_info/
                PushDirectory(new OlympusMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset + 8);
            }
            else if (string.Equals("OLYMPUS\0II", firstTenChars, StringComparison.Ordinal))
            {
                // Olympus Makernote (alternate)
                // Note that data is relative to the beginning of the makernote
                // http://exiv2.org/makernote.html
                PushDirectory(new OlympusMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader.WithShiftedBaseOffset(makernoteOffset), processedIfdOffsets, 12);
            }
            else if (cameraMake != null && cameraMake.StartsWith("MINOLTA", StringComparison.OrdinalIgnoreCase))
            {
                // Cases seen with the model starting with MINOLTA in capitals seem to have a valid Olympus makernote
                // area that commences immediately.
                PushDirectory(new OlympusMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset);
            }
            else if (cameraMake != null && cameraMake.TrimStart().StartsWith("NIKON", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals("Nikon", firstFiveChars, StringComparison.Ordinal))
                {
                    switch (reader.GetByte(makernoteOffset + 6))
                    {
                        case 1:
                        {
                            /* There are two scenarios here:
                             * Type 1:                  **
                             * :0000: 4E 69 6B 6F 6E 00 01 00-05 00 02 00 02 00 06 00 Nikon...........
                             * :0010: 00 00 EC 02 00 00 03 00-03 00 01 00 00 00 06 00 ................
                             * Type 3:                  **
                             * :0000: 4E 69 6B 6F 6E 00 02 00-00 00 4D 4D 00 2A 00 00 Nikon....MM.*...
                             * :0010: 00 08 00 1E 00 01 00 07-00 00 00 04 30 32 30 30 ............0200
                             */
                            PushDirectory(new NikonType1MakernoteDirectory());
                            TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset + 8);
                            break;
                        }
                        case 2:
                        {
                            PushDirectory(new NikonType2MakernoteDirectory());
                            TiffReader.ProcessIfd(this, reader.WithShiftedBaseOffset(makernoteOffset + 10), processedIfdOffsets, 8);
                            break;
                        }
                        default:
                        {
                            Error("Unsupported Nikon makernote data ignored.");
                            break;
                        }
                    }
                }
                else
                {
                    // The IFD begins with the first Makernote byte (no ASCII name).  This occurs with CoolPix 775, E990 and D1 models.
                    PushDirectory(new NikonType2MakernoteDirectory());
                    TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset);
                }
            }
            else if (string.Equals("SONY CAM", firstEightChars, StringComparison.Ordinal) ||
                     string.Equals("SONY DSC", firstEightChars, StringComparison.Ordinal))
            {
                PushDirectory(new SonyType1MakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset + 12);
            }
            // Do this check LAST after most other Sony checks
            else if (cameraMake != null && cameraMake.StartsWith("SONY", StringComparison.Ordinal) &&
                reader.GetBytes(makernoteOffset, 2) != new byte[] { 0x01, 0x00 })
            {
                // The IFD begins with the first Makernote byte (no ASCII name). Used in SR2 and ARW images
                PushDirectory(new SonyType1MakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset);
            }
            else if (string.Equals("SEMC MS\u0000\u0000\u0000\u0000\u0000", firstTwelveChars, StringComparison.Ordinal))
            {
                // Force Motorola byte order for this directory
                // skip 12 byte header + 2 for "MM" + 6
                PushDirectory(new SonyType6MakernoteDirectory());
                TiffReader.ProcessIfd(this, reader.WithByteOrder(isMotorolaByteOrder: true), processedIfdOffsets, makernoteOffset + 20);
            }
            else if (string.Equals("SIGMA\u0000\u0000\u0000", firstEightChars, StringComparison.Ordinal) ||
                     string.Equals("FOVEON\u0000\u0000", firstEightChars, StringComparison.Ordinal))
            {
                PushDirectory(new SigmaMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset + 10);
            }
            else if (string.Equals("KDK", firstThreeChars, StringComparison.Ordinal))
            {
                var directory = new KodakMakernoteDirectory();
                Directories.Add(directory);
                ProcessKodakMakernote(directory, makernoteOffset, reader.WithByteOrder(isMotorolaByteOrder: firstSevenChars == "KDK INFO"));
            }
            else if ("CANON".Equals(cameraMake, StringComparison.OrdinalIgnoreCase))
            {
                PushDirectory(new CanonMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset);
            }
            else if (cameraMake != null && cameraMake.StartsWith("CASIO", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals("QVC\u0000\u0000\u0000", firstSixChars, StringComparison.Ordinal))
                {
                    PushDirectory(new CasioType2MakernoteDirectory());
                    TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset + 6);
                }
                else
                {
                    PushDirectory(new CasioType1MakernoteDirectory());
                    TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset);
                }
            }
            else if (string.Equals("FUJIFILM", firstEightChars, StringComparison.Ordinal) ||
                     string.Equals("FUJIFILM", cameraMake, StringComparison.OrdinalIgnoreCase))
            {
                // Note that this also applies to certain Leica cameras, such as the Digilux-4.3.
                // The 4 bytes after "FUJIFILM" in the makernote point to the start of the makernote
                // IFD, though the offset is relative to the start of the makernote, not the TIFF header
                var makernoteReader = reader.WithShiftedBaseOffset(makernoteOffset).WithByteOrder(isMotorolaByteOrder: false);
                var ifdStart = makernoteReader.GetInt32(8);
                PushDirectory(new FujifilmMakernoteDirectory());
                TiffReader.ProcessIfd(this, makernoteReader, processedIfdOffsets, ifdStart);
            }
            else if (string.Equals("KYOCERA", firstSevenChars, StringComparison.Ordinal))
            {
                // http://www.ozhiker.com/electronics/pjmt/jpeg_info/kyocera_mn.html
                PushDirectory(new KyoceraMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset + 22);
            }
            else if (string.Equals("LEICA", firstFiveChars, StringComparison.Ordinal))
            {
                // used by the X1/X2/X VARIO/T
                // (X1 starts with "LEICA\0\x01\0", Make is "LEICA CAMERA AG")
                // (X2 starts with "LEICA\0\x05\0", Make is "LEICA CAMERA AG")
                // (X VARIO starts with "LEICA\0\x04\0", Make is "LEICA CAMERA AG")
                // (T (Typ 701) starts with "LEICA\0\0x6", Make is "LEICA CAMERA AG")
                // (X (Typ 113) starts with "LEICA\0\0x7", Make is "LEICA CAMERA AG")

                if (string.Equals("LEICA\0\x1\0", firstEightChars, StringComparison.Ordinal) ||
                    string.Equals("LEICA\0\x4\0", firstEightChars, StringComparison.Ordinal) ||
                    string.Equals("LEICA\0\x5\0", firstEightChars, StringComparison.Ordinal) ||
                    string.Equals("LEICA\0\x6\0", firstEightChars, StringComparison.Ordinal) ||
                    string.Equals("LEICA\0\x7\0", firstEightChars, StringComparison.Ordinal))
                {
                    PushDirectory(new LeicaType5MakernoteDirectory());
                    TiffReader.ProcessIfd(this, reader.WithShiftedBaseOffset(makernoteOffset), processedIfdOffsets, 8);
                }
                else if (string.Equals("Leica Camera AG", cameraMake, StringComparison.Ordinal))
                {
                    PushDirectory(new LeicaMakernoteDirectory());
                    TiffReader.ProcessIfd(this, reader.WithByteOrder(isMotorolaByteOrder: false), processedIfdOffsets, makernoteOffset + 8);
                }
                else if (string.Equals("LEICA", cameraMake, StringComparison.Ordinal))
                {
                    // Some Leica cameras use Panasonic makernote tags
                    PushDirectory(new PanasonicMakernoteDirectory());
                    TiffReader.ProcessIfd(this, reader.WithByteOrder(isMotorolaByteOrder: false), processedIfdOffsets, makernoteOffset + 8);
                }
                else
                {
                    return false;
                }
            }
            else if (string.Equals("Panasonic\u0000\u0000\u0000", firstTwelveChars, StringComparison.Ordinal))
            {
                // NON-Standard TIFF IFD Data using Panasonic Tags. There is no Next-IFD pointer after the IFD
                // Offsets are relative to the start of the TIFF header at the beginning of the EXIF segment
                // more information here: http://www.ozhiker.com/electronics/pjmt/jpeg_info/panasonic_mn.html
                PushDirectory(new PanasonicMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset + 12);
            }
            else if (string.Equals("AOC\u0000", firstFourChars, StringComparison.Ordinal))
            {
                // NON-Standard TIFF IFD Data using Casio Type 2 Tags
                // IFD has no Next-IFD pointer at end of IFD, and
                // Offsets are relative to the start of the current IFD tag, not the TIFF header
                // Observed for:
                // - Pentax ist D
                PushDirectory(new CasioType2MakernoteDirectory());
                TiffReader.ProcessIfd(this, reader.WithShiftedBaseOffset(makernoteOffset), processedIfdOffsets, 6);
            }
            else if (cameraMake != null && (cameraMake.StartsWith("PENTAX", StringComparison.OrdinalIgnoreCase) || cameraMake.StartsWith("ASAHI", StringComparison.OrdinalIgnoreCase)))
            {
                // NON-Standard TIFF IFD Data using Pentax Tags
                // IFD has no Next-IFD pointer at end of IFD, and
                // Offsets are relative to the start of the current IFD tag, not the TIFF header
                // Observed for:
                // - PENTAX Optio 330
                // - PENTAX Optio 430
                PushDirectory(new PentaxMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader.WithShiftedBaseOffset(makernoteOffset), processedIfdOffsets, 0);
            }
//          else if ("KC" == firstTwoChars || "MINOL" == firstFiveChars || "MLY" == firstThreeChars || "+M+M+M+M" == firstEightChars)
//          {
//              // This Konica data is not understood.  Header identified in accordance with information at this site:
//              // http://www.ozhiker.com/electronics/pjmt/jpeg_info/minolta_mn.html
//              // TODO add support for minolta/konica cameras
//              exifDirectory.addError("Unsupported Konica/Minolta data ignored.");
//          }
            else if (string.Equals("SANYO\x0\x1\x0", firstEightChars, StringComparison.Ordinal))
            {
                PushDirectory(new SanyoMakernoteDirectory());
                TiffReader.ProcessIfd(this, reader.WithShiftedBaseOffset(makernoteOffset), processedIfdOffsets, 8);
            }
            else if (cameraMake != null && cameraMake.StartsWith("RICOH", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(firstTwoChars, "Rv", StringComparison.Ordinal) ||
                    string.Equals(firstThreeChars, "Rev", StringComparison.Ordinal))
                {
                    // This is a textual format, where the makernote bytes look like:
                    //   Rv0103;Rg1C;Bg18;Ll0;Ld0;Aj0000;Bn0473800;Fp2E00:������������������������������
                    //   Rv0103;Rg1C;Bg18;Ll0;Ld0;Aj0000;Bn0473800;Fp2D05:������������������������������
                    //   Rv0207;Sf6C84;Rg76;Bg60;Gg42;Ll0;Ld0;Aj0004;Bn0B02900;Fp10B8;Md6700;Ln116900086D27;Sv263:0000000000000000000000��
                    // This format is currently unsupported
                    return false;
                }
                if (firstFiveChars.Equals("RICOH", StringComparison.OrdinalIgnoreCase))
                {
                    PushDirectory(new RicohMakernoteDirectory());
                    // Always in Motorola byte order
                    TiffReader.ProcessIfd(this, reader.WithByteOrder(isMotorolaByteOrder: true).WithShiftedBaseOffset(makernoteOffset), processedIfdOffsets, 8);
                }
            }
            else if (string.Equals(firstTenChars, "Apple iOS\0", StringComparison.Ordinal))
            {
                PushDirectory(new AppleMakernoteDirectory());
                // Always in Motorola byte order
                TiffReader.ProcessIfd(this, reader.WithByteOrder(isMotorolaByteOrder: true).WithShiftedBaseOffset(makernoteOffset), processedIfdOffsets, 14);
            }
            else if (reader.GetUInt16(makernoteOffset) == ReconyxHyperFireMakernoteDirectory.MakernoteVersion)
            {
                var directory = new ReconyxHyperFireMakernoteDirectory();
                Directories.Add(directory);
                ProcessReconyxHyperFireMakernote(directory, makernoteOffset, reader);
            }
            else if (string.Equals("RECONYXUF", firstNineChars, StringComparison.OrdinalIgnoreCase))
            {
                var directory = new ReconyxUltraFireMakernoteDirectory();
                Directories.Add(directory);
                ProcessReconyxUltraFireMakernote(directory, makernoteOffset, reader);
            }
            else if (string.Equals("SAMSUNG", cameraMake, StringComparison.Ordinal))
            {
                // Only handles Type2 notes correctly. Others aren't implemented, and it's complex to determine which ones to use
                PushDirectory(new SamsungType2MakernoteDirectory());
                TiffReader.ProcessIfd(this, reader, processedIfdOffsets, makernoteOffset);
            }
            else
            {
                // The makernote is not comprehended by this library.
                // If you are reading this and believe a particular camera's image should be processed, get in touch.
                return false;
            }

            return true;
#else
            return false;
#endif
        }

        private static bool HandlePrintIM([NotNull] Directory directory, int tagId)
        {
            if (tagId == ExifDirectoryBase.TagPrintImageMatchingInfo)
                return true;

#if METADATAEXTRACTOR_HAVE_MAKER_NOTES
            if (tagId == 0x0E00)
            {
                // Tempting to say every tagid of 0x0E00 is a PIM tag, but can't be 100% sure
                if (directory is CasioType2MakernoteDirectory ||
                    directory is KyoceraMakernoteDirectory ||
                    directory is NikonType2MakernoteDirectory ||
                    directory is OlympusMakernoteDirectory ||
                    directory is PanasonicMakernoteDirectory ||
                    directory is PentaxMakernoteDirectory ||
                    directory is RicohMakernoteDirectory ||
                    directory is SanyoMakernoteDirectory ||
                    directory is SonyType1MakernoteDirectory)
                    return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// Process PrintIM IFD
        /// </summary>
        /// <remarks>
        /// Converted from Exiftool version 10.33 created by Phil Harvey
        /// http://www.sno.phy.queensu.ca/~phil/exiftool/
        /// lib\Image\ExifTool\PrintIM.pm
        /// </remarks>
        private static void ProcessPrintIM([NotNull] PrintIMDirectory directory, int tagValueOffset, [NotNull] IndexedReader reader, int byteCount)
        {
            if (byteCount == 0)
            {
                directory.AddError("Empty PrintIM data");
                return;
            }

            if (byteCount <= 15)
            {
                directory.AddError("Bad PrintIM data");
                return;
            }

            var header = reader.GetString(tagValueOffset, 12, Encoding.UTF8);

            if (!header.StartsWith("PrintIM", StringComparison.Ordinal))
            {
                directory.AddError("Invalid PrintIM header");
                return;
            }

            var localReader = reader;
            // check size of PrintIM block
            var num = localReader.GetUInt16(tagValueOffset + 14);
            if (byteCount < 16 + num * 6)
            {
                // size is too big, maybe byte ordering is wrong
                localReader = reader.WithByteOrder(!reader.IsMotorolaByteOrder);
                num = localReader.GetUInt16(tagValueOffset + 14);
                if (byteCount < 16 + num * 6)
                {
                    directory.AddError("Bad PrintIM size");
                    return;
                }
            }

            directory.Set(PrintIMDirectory.TagPrintImVersion, header.Substring(8, 4));

            for (var n = 0; n < num; n++)
            {
                var pos = tagValueOffset + 16 + n * 6;
                var tag = localReader.GetUInt16(pos);
                var val = localReader.GetUInt32(pos + 2);

                directory.Set(tag, val);
            }
        }

        private static void ProcessBinary([NotNull] Directory directory, int tagValueOffset, [NotNull] IndexedReader reader, int byteCount, bool issigned = true, int arrayLength = 1)
        {
            // expects signed/unsigned int16 (for now)
            var byteSize = issigned ? sizeof(short) : sizeof(ushort);

            // 'directory' is assumed to contain tags that correspond to the byte position unless it's a set of bytes
            for (var i = 0; i < byteCount; i++)
            {
                if (directory.HasTagName(i))
                {
                    // only process this tag if the 'next' integral tag exists. Otherwise, it's a set of bytes
                    if (i < byteCount - 1 && directory.HasTagName(i + 1))
                    {
                        if (issigned)
                            directory.Set(i, reader.GetInt16(tagValueOffset + i*byteSize));
                        else
                            directory.Set(i, reader.GetUInt16(tagValueOffset + i*byteSize));
                    }
                    else
                    {
                        // the next arrayLength bytes are a multi-byte value
                        if (issigned)
                        {
                            var val = new short[arrayLength];
                            for (var j = 0; j < val.Length; j++)
                                val[j] = reader.GetInt16(tagValueOffset + (i + j)*byteSize);
                            directory.Set(i, val);
                        }
                        else
                        {
                            var val = new ushort[arrayLength];
                            for (var j = 0; j < val.Length; j++)
                                val[j] = reader.GetUInt16(tagValueOffset + (i + j)*byteSize);
                            directory.Set(i, val);
                        }

                        i += arrayLength - 1;
                    }
                }

            }
        }

#if METADATAEXTRACTOR_HAVE_MAKER_NOTES
        private static void ProcessKodakMakernote([NotNull] KodakMakernoteDirectory directory, int tagValueOffset, [NotNull] IndexedReader reader)
        {
            // Kodak's makernote is not in IFD format. It has values at fixed offsets.
            var dataOffset = tagValueOffset + 8;
            try
            {
                directory.Set(KodakMakernoteDirectory.TagKodakModel,           reader.GetString(dataOffset, 8, Encoding.UTF8));
                directory.Set(KodakMakernoteDirectory.TagQuality,              reader.GetByte(dataOffset + 9));
                directory.Set(KodakMakernoteDirectory.TagBurstMode,            reader.GetByte(dataOffset + 10));
                directory.Set(KodakMakernoteDirectory.TagImageWidth,           reader.GetUInt16(dataOffset + 12));
                directory.Set(KodakMakernoteDirectory.TagImageHeight,          reader.GetUInt16(dataOffset + 14));
                directory.Set(KodakMakernoteDirectory.TagYearCreated,          reader.GetUInt16(dataOffset + 16));
                directory.Set(KodakMakernoteDirectory.TagMonthDayCreated,      reader.GetBytes(dataOffset + 18, 2));
                directory.Set(KodakMakernoteDirectory.TagTimeCreated,          reader.GetBytes(dataOffset + 20, 4));
                directory.Set(KodakMakernoteDirectory.TagBurstMode2,           reader.GetUInt16(dataOffset + 24));
                directory.Set(KodakMakernoteDirectory.TagShutterMode,          reader.GetByte(dataOffset + 27));
                directory.Set(KodakMakernoteDirectory.TagMeteringMode,         reader.GetByte(dataOffset + 28));
                directory.Set(KodakMakernoteDirectory.TagSequenceNumber,       reader.GetByte(dataOffset + 29));
                directory.Set(KodakMakernoteDirectory.TagFNumber,              reader.GetUInt16(dataOffset + 30));
                directory.Set(KodakMakernoteDirectory.TagExposureTime,         reader.GetUInt32(dataOffset + 32));
                directory.Set(KodakMakernoteDirectory.TagExposureCompensation, reader.GetInt16(dataOffset + 36));
                directory.Set(KodakMakernoteDirectory.TagFocusMode,            reader.GetByte(dataOffset + 56));
                directory.Set(KodakMakernoteDirectory.TagWhiteBalance,         reader.GetByte(dataOffset + 64));
                directory.Set(KodakMakernoteDirectory.TagFlashMode,            reader.GetByte(dataOffset + 92));
                directory.Set(KodakMakernoteDirectory.TagFlashFired,           reader.GetByte(dataOffset + 93));
                directory.Set(KodakMakernoteDirectory.TagIsoSetting,           reader.GetUInt16(dataOffset + 94));
                directory.Set(KodakMakernoteDirectory.TagIso,                  reader.GetUInt16(dataOffset + 96));
                directory.Set(KodakMakernoteDirectory.TagTotalZoom,            reader.GetUInt16(dataOffset + 98));
                directory.Set(KodakMakernoteDirectory.TagDateTimeStamp,        reader.GetUInt16(dataOffset + 100));
                directory.Set(KodakMakernoteDirectory.TagColorMode,            reader.GetUInt16(dataOffset + 102));
                directory.Set(KodakMakernoteDirectory.TagDigitalZoom,          reader.GetUInt16(dataOffset + 104));
                directory.Set(KodakMakernoteDirectory.TagSharpness,            reader.GetSByte(dataOffset + 107));
            }
            catch (IOException ex)
            {
                directory.AddError("Error processing Kodak makernote data: " + ex.Message);
            }
        }

        private static void ProcessReconyxHyperFireMakernote([NotNull] ReconyxHyperFireMakernoteDirectory directory, int makernoteOffset, [NotNull] IndexedReader reader)
        {
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagMakernoteVersion, reader.GetUInt16(makernoteOffset));

            // revision and build are reversed from .NET ordering
            ushort major = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagFirmwareVersion);
            ushort minor = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagFirmwareVersion + 2);
            ushort revision = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagFirmwareVersion + 4);
            string buildYear = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagFirmwareVersion + 6).ToString("x4");
            string buildDate = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagFirmwareVersion + 8).ToString("x4");
            string buildYearAndDate = buildYear + buildDate;
            if (int.TryParse(buildYear + buildDate, out int build))
            {
                directory.Set(ReconyxHyperFireMakernoteDirectory.TagFirmwareVersion, new Version(major, minor, revision, build));
            }
            else
            {
                directory.Set(ReconyxHyperFireMakernoteDirectory.TagFirmwareVersion, new Version(major, minor, revision));
                directory.AddError("Error processing Reconyx HyperFire makernote data: build '" + buildYearAndDate + "' is not in the expected format and will be omitted from Firmware Version.");
            }

            directory.Set(ReconyxHyperFireMakernoteDirectory.TagTriggerMode, new string((char)reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagTriggerMode), 1));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagSequence,
                          new[]
                          {
                              reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagSequence),
                              reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagSequence + 2)
                          });

            uint eventNumberHigh = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagEventNumber);
            uint eventNumberLow = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagEventNumber + 2);
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagEventNumber, (eventNumberHigh << 16) + eventNumberLow);

            ushort seconds = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagDateTimeOriginal);
            ushort minutes = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagDateTimeOriginal + 2);
            ushort hour = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagDateTimeOriginal + 4);
            ushort month = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagDateTimeOriginal + 6);
            ushort day = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagDateTimeOriginal + 8);
            ushort year = reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagDateTimeOriginal + 10);
            if (seconds < 60 &&
                minutes < 60 &&
                hour < 24 &&
                month >= 1 && month < 13 &&
                day >= 1 && day < 32 &&
                year >= DateTime.MinValue.Year && year <= DateTime.MaxValue.Year)
            {
                directory.Set(ReconyxHyperFireMakernoteDirectory.TagDateTimeOriginal, new DateTime(year, month, day, hour, minutes, seconds, DateTimeKind.Unspecified));
            }
            else
            {
                directory.AddError("Error processing Reconyx HyperFire makernote data: Date/Time Original " + year + "-" + month + "-" + day + " " + hour + ":" + minutes + ":" + seconds + " is not a valid date/time.");
            }

            directory.Set(ReconyxHyperFireMakernoteDirectory.TagMoonPhase, reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagMoonPhase));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagAmbientTemperatureFahrenheit, reader.GetInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagAmbientTemperatureFahrenheit));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagAmbientTemperature, reader.GetInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagAmbientTemperature));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagSerialNumber, reader.GetString(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagSerialNumber, 28, Encoding.Unicode));
            // two unread bytes: the serial number's terminating null

            directory.Set(ReconyxHyperFireMakernoteDirectory.TagContrast, reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagContrast));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagBrightness, reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagBrightness));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagSharpness, reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagSharpness));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagSaturation, reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagSaturation));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagInfraredIlluminator, reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagInfraredIlluminator));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagMotionSensitivity, reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagMotionSensitivity));
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagBatteryVoltage, reader.GetUInt16(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagBatteryVoltage) / 1000.0);
            directory.Set(ReconyxHyperFireMakernoteDirectory.TagUserLabel, reader.GetNullTerminatedString(makernoteOffset + ReconyxHyperFireMakernoteDirectory.TagUserLabel, 44));
        }

        private static void ProcessReconyxUltraFireMakernote([NotNull] ReconyxUltraFireMakernoteDirectory directory, int makernoteOffset, [NotNull] IndexedReader reader)
        {
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagLabel, reader.GetString(makernoteOffset, 9, Encoding.UTF8));
            uint makernoteID = ByteConvert.FromBigEndianToNative(reader.GetUInt32(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagMakernoteID));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagMakernoteID, makernoteID);
            if (makernoteID != ReconyxUltraFireMakernoteDirectory.MakernoteID)
            {
                directory.AddError("Error processing Reconyx UltraFire makernote data: unknown Makernote ID 0x" + makernoteID.ToString("x8"));
                return;
            }
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagMakernoteSize, ByteConvert.FromBigEndianToNative(reader.GetUInt32(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagMakernoteSize)));
            uint makernotePublicID = ByteConvert.FromBigEndianToNative(reader.GetUInt32(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagMakernotePublicID));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagMakernotePublicID, makernotePublicID);
            if (makernotePublicID != ReconyxUltraFireMakernoteDirectory.MakernotePublicID)
            {
                directory.AddError("Error processing Reconyx UltraFire makernote data: unknown Makernote Public ID 0x" + makernotePublicID.ToString("x8"));
                return;
            }
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagMakernotePublicSize, ByteConvert.FromBigEndianToNative(reader.GetUInt16(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagMakernotePublicSize)));

            directory.Set(ReconyxUltraFireMakernoteDirectory.TagCameraVersion, ProcessReconyxUltraFireVersion(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagCameraVersion, reader));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagUibVersion, ProcessReconyxUltraFireVersion(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagUibVersion, reader));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagBtlVersion, ProcessReconyxUltraFireVersion(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagBtlVersion, reader));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagPexVersion, ProcessReconyxUltraFireVersion(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagPexVersion, reader));

            directory.Set(ReconyxUltraFireMakernoteDirectory.TagEventType, reader.GetString(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagEventType, 1, Encoding.UTF8));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagSequence,
                          new[]
                          {
                              reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagSequence),
                              reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagSequence + 1)
                          });
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagEventNumber, ByteConvert.FromBigEndianToNative(reader.GetUInt32(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagEventNumber)));

            byte seconds = reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagDateTimeOriginal);
            byte minutes = reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagDateTimeOriginal + 1);
            byte hour = reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagDateTimeOriginal + 2);
            byte day = reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagDateTimeOriginal + 3);
            byte month = reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagDateTimeOriginal + 4);
            ushort year = ByteConvert.FromBigEndianToNative(reader.GetUInt16(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagDateTimeOriginal + 5));
            if (seconds < 60 &&
                minutes < 60 &&
                hour < 24 &&
                month >= 1 && month < 13 &&
                day >= 1 && day < 32 &&
                year >= DateTime.MinValue.Year && year <= DateTime.MaxValue.Year)
            {
                directory.Set(ReconyxUltraFireMakernoteDirectory.TagDateTimeOriginal, new DateTime(year, month, day, hour, minutes, seconds, DateTimeKind.Unspecified));
            }
            else
            {
                directory.AddError("Error processing Reconyx UltraFire makernote data: Date/Time Original " + year + "-" + month + "-" + day + " " + hour + ":" + minutes + ":" + seconds + " is not a valid date/time.");
            }
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagDayOfWeek, reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagDayOfWeek));

            directory.Set(ReconyxUltraFireMakernoteDirectory.TagMoonPhase, reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagMoonPhase));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagAmbientTemperatureFahrenheit, ByteConvert.FromBigEndianToNative(reader.GetInt16(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagAmbientTemperatureFahrenheit)));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagAmbientTemperature, ByteConvert.FromBigEndianToNative(reader.GetInt16(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagAmbientTemperature)));

            directory.Set(ReconyxUltraFireMakernoteDirectory.TagFlash, reader.GetByte(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagFlash));
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagBatteryVoltage, ByteConvert.FromBigEndianToNative(reader.GetUInt16(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagBatteryVoltage)) / 1000.0);
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagSerialNumber, reader.GetString(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagSerialNumber, 14, Encoding.UTF8));
            // unread byte: the serial number's terminating null
            directory.Set(ReconyxUltraFireMakernoteDirectory.TagUserLabel, reader.GetNullTerminatedString(makernoteOffset + ReconyxUltraFireMakernoteDirectory.TagUserLabel, 20, Encoding.UTF8));
        }

        private static string ProcessReconyxUltraFireVersion(int versionOffset, [NotNull] IndexedReader reader)
        {
            string major = reader.GetByte(versionOffset).ToString();
            string minor = reader.GetByte(versionOffset + 1).ToString();
            string year = ByteConvert.FromBigEndianToNative(reader.GetUInt16(versionOffset + 2)).ToString("x4");
            string month = reader.GetByte(versionOffset + 4).ToString("x2");
            string day = reader.GetByte(versionOffset + 5).ToString("x2");
            string revision = reader.GetString(versionOffset + 6, 1, Encoding.UTF8);
            return major + "." + minor + "." + year + "." + month + "." + day + revision;
        }
#endif
    }

    /// <summary>
    /// Decodes Exif binary data into potentially many <see cref="Directory"/> objects such as
    /// <see cref="ExifSubIfdDirectory"/>, <see cref="ExifThumbnailDirectory"/>, <see cref="ExifInteropDirectory"/>,
    /// <see cref="GpsDirectory"/>, camera makernote directories and more.
    /// </summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class ExifReader : IJpegSegmentMetadataReader
    {
        /// <summary>Exif data stored in JPEG files' APP1 segment are preceded by this six character preamble.</summary>
        public const string JpegSegmentPreamble = "Exif\x0\x0";

        ICollection<JpegSegmentType> IJpegSegmentMetadataReader.SegmentTypes { get { return new [] { JpegSegmentType.App1 }; } }

        public DirectoryList ReadJpegSegments(IEnumerable<JpegSegment> segments)
        {
            //return segments
            //    .Where(segment => segment.Bytes.Length >= JpegSegmentPreamble.Length && Encoding.UTF8.GetString(segment.Bytes, 0, JpegSegmentPreamble.Length) == JpegSegmentPreamble)
            //    .SelectMany(segment => Extract(new ByteArrayReader(segment.Bytes, baseOffset: JpegSegmentPreamble.Length)))
            //    .ToList();
            List<Directory> res = new List<Directory>();
            foreach (var segment in segments)
                if (segment.Bytes.Length >= JpegSegmentPreamble.Length && Encoding.UTF8.GetString(segment.Bytes, 0, JpegSegmentPreamble.Length) == JpegSegmentPreamble)
                    res.AddRange(Extract(new ByteArrayReader(segment.Bytes, baseOffset: JpegSegmentPreamble.Length)));
            return res;
        }

        /// <summary>
        /// Reads TIFF formatted Exif data a specified offset within a <see cref="IndexedReader"/>.
        /// </summary>
        [NotNull]
        public DirectoryList Extract([NotNull] IndexedReader reader)
        {
            var directories = new List<Directory>();
            var exifTiffHandler = new ExifTiffHandler(directories);

            try
            {
                // Read the TIFF-formatted Exif data
                TiffReader.ProcessTiff(reader, exifTiffHandler);
            }
            catch (Exception e)
            {
                exifTiffHandler.Error("Exception processing TIFF data: " + e.Message);
            }

            return directories;
        }
    }
}

namespace MetadataExtractor.Formats.Jpeg
{
    /// <summary>An enumeration of the known segment types found in JPEG files.</summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>http://www.ozhiker.com/electronics/pjmt/jpeg_info/app_segments.html</item>
    ///   <item>http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/JPEG.html</item>
    ///   <item>http://lad.dsc.ufcg.edu.br/multimidia/jpegmarker.pdf</item>
    ///   <item>http://dev.exiv2.org/projects/exiv2/wiki/The_Metadata_in_JPEG_files</item>
    /// </list>
    /// </remarks>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum JpegSegmentType : byte
    {
        /// <summary>For temporary use in arithmetic coding.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Tem = 0x01,

        /// <summary>Start Of Image segment. Begins the compressed JPEG data stream.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Soi = 0xD8,

        /// <summary>Define Quantization Table.</summary>
        /// <remarks>Specifies one or more quantization tables.</remarks>
        Dqt = 0xDB,

        /// <summary>Start-of-Frame, non-differential Huffman coding frame, baseline DCT.</summary>
        /// <remarks>
        /// Indicates that this is a baseline DCT-based JPEG, and specifies the width,
        /// height, number of components, and component subsampling (e.g., 4:2:0).
        /// </remarks>
        Sof0 = 0xC0,

        /// <summary>Start-of-Frame, non-differential Huffman coding frame, extended sequential DCT.</summary>
        Sof1 = 0xC1,

        /// <summary>Start-of-Frame, non-differential Huffman coding frame, progressive DCT.</summary>
        /// <remarks>
        /// Indicates that this is a progressive DCT-based JPEG, and specifies the width,
        /// height, number of components, and component subsampling (e.g., 4:2:0).
        /// </remarks>
        Sof2 = 0xC2,

        /// <summary>Start-of-Frame, non-differential Huffman coding frame, lossless sequential.</summary>
        Sof3 = 0xC3,

        /// <summary>Define Huffman Table(s).</summary>
        /// <remarks>Specifies one or more Huffman tables.</remarks>
        Dht = 0xC4,

        /// <summary>Start-of-Frame, differential Huffman coding frame, differential sequential DCT.</summary>
        Sof5 = 0xC5,

        /// <summary>Start-of-Frame, differential Huffman coding frame, differential progressive DCT.</summary>
        Sof6 = 0xC6,

        /// <summary>Start-of-Frame, differential Huffman coding frame, differential lossless.</summary>
        Sof7 = 0xC7,

        /// <summary>Start-of-Frame, non-differential arithmetic coding frame, extended sequential DCT.</summary>
        Sof9 = 0xC9,

        /// <summary>Start-of-Frame, non-differential arithmetic coding frame, progressive DCT.</summary>
        Sof10 = 0xCA,

        /// <summary>Start-of-Frame, non-differential arithmetic coding frame, lossless sequential.</summary>
        Sof11 = 0xCB,

        /// <summary>Define Arithmetic Coding table(s).</summary>
        Dac = 0xCC,

        /// <summary>Start-of-Frame, differential arithmetic coding frame, differential sequential DCT.</summary>
        Sof13 = 0xCD,

        /// <summary>Start-of-Frame, differential arithmetic coding frame, differential progressive DCT.</summary>
        Sof14 = 0xCE,

        /// <summary>Start-of-Frame, differential arithmetic coding frame, differential lossless.</summary>
        Sof15 = 0xCF,

        /// <summary>Restart.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Rst0 = 0xD0,

        /// <summary>Restart.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Rst1 = 0xD1,

        /// <summary>Restart.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Rst2 = 0xD2,

        /// <summary>Restart.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Rst3 = 0xD3,

        /// <summary>Restart.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Rst4 = 0xD4,

        /// <summary>Restart.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Rst5 = 0xD5,

        /// <summary>Restart.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Rst6 = 0xD6,

        /// <summary>Restart.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Rst7 = 0xD7,

        /// <summary>End-of-Image. Terminates the JPEG compressed data stream that started at <see cref="Soi"/>.</summary>
        /// <remarks>No length or parameter sequence follows this marker.</remarks>
        Eoi = 0xD9,

        /// <summary>Start-of-Scan.</summary>
        /// <remarks>
        /// Begins a top-to-bottom scan of the image.
        /// In baseline DCT JPEG images, there is generally a single scan.
        /// Progressive DCT JPEG images usually contain multiple scans.
        /// This marker specifies which slice of data it will contain, and is
        /// immediately followed by entropy-coded data.
        /// </remarks>
        Sos = 0xDA,

        /// <summary>Define Number of Lines.</summary>
        Dnl = 0xDC,

        /// <summary>Define Restart Interval.</summary>
        /// <remarks>
        /// Specifies the interval between RSTn markers, in macroblocks.
        /// This marker is followed by two bytes indicating the fixed size so
        /// it can be treated like any other variable size segment.
        /// </remarks>
        Dri = 0xDD,

        /// <summary>Define Hierarchical Progression.</summary>
        Dhp = 0xDE,

        /// <summary>Expand reference components.</summary>
        Exp = 0xDF,

        /// <summary>Application specific, type 0. Commonly contains JFIF, JFXX.</summary>
        App0 = 0xE0,

        /// <summary>Application specific, type 1. Commonly contains Exif. XMP data is also kept in here, though usually in a second instance.</summary>
        App1 = 0xE1,

        /// <summary>Application specific, type 2. Commonly contains ICC.</summary>
        App2 = 0xE2,

        /// <summary>Application specific, type 3.</summary>
        App3 = 0xE3,

        /// <summary>Application specific, type 4.</summary>
        App4 = 0xE4,

        /// <summary>Application specific, type 5.</summary>
        App5 = 0xE5,

        /// <summary>Application specific, type 6.</summary>
        App6 = 0xE6,

        /// <summary>Application specific, type 7.</summary>
        App7 = 0xE7,

        /// <summary>Application specific, type 8.</summary>
        App8 = 0xE8,

        /// <summary>Application specific, type 9.</summary>
        App9 = 0xE9,

        /// <summary>Application specific, type A. Can contain Unicode comments, though <see cref="Com"/> is more commonly used for comments.</summary>
        AppA = 0xEA,

        /// <summary>Application specific, type B.</summary>
        AppB = 0xEB,

        /// <summary>Application specific, type C.</summary>
        AppC = 0xEC,

        /// <summary>Application specific, type D. Commonly contains IPTC, Photoshop data.</summary>
        AppD = 0xED,

        /// <summary>Application specific, type E. Commonly contains Adobe data.</summary>
        AppE = 0xEE,

        /// <summary>Application specific, type F.</summary>
        AppF = 0xEF,

        /// <summary>JPEG comment (text).</summary>
        Com = 0xFE
    }

    /// <summary>
    /// Extension methods for <see cref="JpegSegmentType"/> enum.
    /// </summary>
    public static class JpegSegmentTypeExtensions
    {
        /// <summary>Gets whether this JPEG segment type might contain metadata.</summary>
        /// <remarks>Used to exclude large image-data-only segment from certain types of processing.</remarks>
        public static bool CanContainMetadata(this JpegSegmentType type)
        {
            switch (type)
            {
                case JpegSegmentType.Soi:
                case JpegSegmentType.Dqt:
                case JpegSegmentType.Dht:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Gets JPEG segment types that might contain metadata.</summary>
#if DOTNET35
        public static IEnumerable<JpegSegmentType> CanContainMetadataTypes
#else
        public static IReadOnlyList<JpegSegmentType> CanContainMetadataTypes
#endif
        {get{ 
            List<JpegSegmentType> res = new List<JpegSegmentType>();
            foreach (object o in Enum.GetValues(typeof(JpegSegmentType)))
                if (((JpegSegmentType)o).CanContainMetadata())
                    res.Add((JpegSegmentType)o);
            return res;
        }}

        /// <summary>Gets whether this JPEG segment type's marker is followed by a length indicator.</summary>
        public static bool ContainsPayload(this JpegSegmentType type)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (type)
            {
                case JpegSegmentType.Soi:
                case JpegSegmentType.Eoi:
                case JpegSegmentType.Rst0:
                case JpegSegmentType.Rst1:
                case JpegSegmentType.Rst2:
                case JpegSegmentType.Rst3:
                case JpegSegmentType.Rst4:
                case JpegSegmentType.Rst5:
                case JpegSegmentType.Rst6:
                case JpegSegmentType.Rst7:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>Gets whether this JPEG segment is intended to hold application specific data.</summary>
        public static bool IsApplicationSpecific(this JpegSegmentType type) { return type >= JpegSegmentType.App0 && type <= JpegSegmentType.AppF; }
    }

    /// <summary>
    /// Holds information about a JPEG segment.
    /// </summary>
    /// <seealso cref="JpegSegmentReader"/>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class JpegSegment
    {
        public JpegSegmentType Type { get { return _Type; } }
        JpegSegmentType _Type;
        [NotNull] public byte[] Bytes { get { return _Bytes; } }
        byte[] _Bytes;
        public long Offset { get { return _Offset; } }
        long _Offset;

        public JpegSegment(JpegSegmentType type, [NotNull] byte[] bytes, long offset)
        {
            _Type = type;
            _Bytes = bytes;
            _Offset = offset;
        }
    }

    /// <summary>Defines an object that extracts metadata from in JPEG segments.</summary>
    public interface IJpegSegmentMetadataReader
    {
        /// <summary>Gets the set of JPEG segment types that this reader is interested in.</summary>
        [NotNull]
        ICollection<JpegSegmentType> SegmentTypes { get; }

        /// <summary>Extracts metadata from all instances of a particular JPEG segment type.</summary>
        /// <param name="segments">
        /// A sequence of JPEG segments from which the metadata should be extracted. These are in the order encountered in the original file.
        /// </param>
        [NotNull]
        DirectoryList ReadJpegSegments([NotNull] IEnumerable<JpegSegment> segments);
    }

    /// <summary>An exception class thrown upon unexpected and fatal conditions while processing a JPEG file.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class JpegProcessingException : ImageProcessingException
    {
        public JpegProcessingException([CanBeNull] string message)
            : base(message)
        {
        }

        public JpegProcessingException([CanBeNull] string message, [CanBeNull] Exception innerException)
            : base(message, innerException)
        {
        }

        public JpegProcessingException([CanBeNull] Exception innerException)
            : base(innerException)
        {
        }

#if !NETSTANDARD1_3
        protected JpegProcessingException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <summary>Parses the structure of JPEG data, returning contained segments.</summary>
    /// <remarks>
    /// JPEG files are composed of a sequence of consecutive JPEG segments. Each segment has a type <see cref="JpegSegmentType"/>.
    /// A JPEG file can contain multiple segments having the same type.
    /// <para />
    /// Segments are returned in the order they appear in the file, however that order may vary from file to file.
    /// <para />
    /// Use <see cref="ReadSegments(SequentialReader,ICollection{JpegSegmentType})"/> to specific segment types,
    /// or pass <c>null</c> to read all segments.
    /// <para />
    /// Note that SOS (start of scan) or EOI (end of image) segments are not returned by this class's methods.
    /// </remarks>
    /// <seealso cref="JpegSegment"/>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public static class JpegSegmentReader
    {
        /// <summary>
        /// Walks the provided JPEG data, returning <see cref="JpegSegment"/> objects.
        /// </summary>
        /// <remarks>
        /// Will not return SOS (start of scan) or EOI (end of image) segments.
        /// </remarks>
        /// <param name="filePath">a file from which the JPEG data will be read.</param>
        /// <param name="segmentTypes">the set of JPEG segments types that are to be returned. If this argument is <c>null</c> then all found segment types are returned.</param>
        /// <exception cref="JpegProcessingException"/>
        /// <exception cref="IOException"/>
        [NotNull]
        public static IEnumerable<JpegSegment> ReadSegments([NotNull] string filePath, [CanBeNull] ICollection<JpegSegmentType> segmentTypes = null)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return ReadSegments(new SequentialStreamReader(stream), segmentTypes);
        }

        /// <summary>
        /// Processes the provided JPEG data, and extracts the specified JPEG segments into a <see cref="JpegSegment"/> object.
        /// </summary>
        /// <remarks>
        /// Will not return SOS (start of scan) or EOI (end of image) segments.
        /// </remarks>
        /// <param name="reader">a <see cref="SequentialReader"/> from which the JPEG data will be read. It must be positioned at the beginning of the JPEG data stream.</param>
        /// <param name="segmentTypes">the set of JPEG segments types that are to be returned. If this argument is <c>null</c> then all found segment types are returned.</param>
        /// <exception cref="JpegProcessingException"/>
        /// <exception cref="IOException"/>
        [NotNull]
        public static IEnumerable<JpegSegment> ReadSegments([NotNull] SequentialReader reader, [CanBeNull] ICollection<JpegSegmentType> segmentTypes = null)
        {
            if (!reader.IsMotorolaByteOrder)
                throw new JpegProcessingException("Must be big-endian/Motorola byte order.");

            // first two bytes should be JPEG magic number
            var magicNumber = reader.GetUInt16();

            if (magicNumber != 0xFFD8)
                throw new JpegProcessingException(string.Format("JPEG data is expected to begin with 0xFFD8 (ÿØ) not 0x{0:X4}", magicNumber));

            do
            {
                // Find the segment marker. Markers are zero or more 0xFF bytes, followed
                // by a 0xFF and then a byte not equal to 0x00 or 0xFF.
                var segmentIdentifier = reader.GetByte();
                var segmentTypeByte = reader.GetByte();

                // Read until we have a 0xFF byte followed by a byte that is not 0xFF or 0x00
                while (segmentIdentifier != 0xFF || segmentTypeByte == 0xFF || segmentTypeByte == 0)
                {
                    segmentIdentifier = segmentTypeByte;
                    segmentTypeByte = reader.GetByte();
                }

                var segmentType = (JpegSegmentType)segmentTypeByte;

                if (segmentType == JpegSegmentType.Sos)
                {
                    // The 'Start-Of-Scan' segment's length doesn't include the image data, instead would
                    // have to search for the two bytes: 0xFF 0xD9 (EOI).
                    // It comes last so simply return at this point
                    yield break;
                }

                if (segmentType == JpegSegmentType.Eoi)
                {
                    // the 'End-Of-Image' segment -- this should never be found in this fashion
                    yield break;
                }

                // next 2-bytes are <segment-size>: [high-byte] [low-byte]
                var segmentLength = (int)reader.GetUInt16();

                // segment length includes size bytes, so subtract two
                segmentLength -= 2;

                // TODO exception strings should end with periods
                if (segmentLength < 0)
                    throw new JpegProcessingException("JPEG segment size would be less than zero");

                // Check whether we are interested in this segment
                if (segmentTypes == null || segmentTypes.Contains(segmentType))
                {
                    var segmentOffset = reader.Position;
                    var segmentBytes = reader.GetBytes(segmentLength);
                    Debug.Assert(segmentLength == segmentBytes.Length);
                    yield return new JpegSegment(segmentType, segmentBytes, segmentOffset);
                }
                else
                {
                    // Some of the JPEG is truncated, so just return what data we've already gathered
                    if (!reader.TrySkip(segmentLength))
                        yield break;
                }
            }
            while (true);
        }
    }

    /// <summary>Obtains all available metadata from JPEG formatted files.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public static class JpegMetadataReader
    {
        private static readonly ICollection<IJpegSegmentMetadataReader> _allReaders = new IJpegSegmentMetadataReader[]
        {
            //new JpegReader(),
            //new JpegCommentReader(),
            //new JfifReader(),
            //new JfxxReader(),
            new MetadataExtractor.Formats.Exif.ExifReader(),
            //new XmpReader(),
            //new IccReader(),
            //new PhotoshopReader(),
            //new DuckyReader(),
            //new IptcReader(),
            //new AdobeJpegReader()
        };

        /// <exception cref="JpegProcessingException"/>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] Stream stream, [CanBeNull] ICollection<IJpegSegmentMetadataReader> readers = null)
        {
            return Process(stream, readers);
        }

        /// <exception cref="JpegProcessingException"/>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] string filePath, [CanBeNull] ICollection<IJpegSegmentMetadataReader> readers = null)
        {
            var directories = new List<Directory>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                directories.AddRange(ReadMetadata(stream, readers));

#if METADATAEXTRACTOR_HAVE_FILEMETADATA
            directories.Add(new FileMetadataReader().Read(filePath));
#endif

            return directories;
        }

        /// <exception cref="JpegProcessingException"/>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public static DirectoryList Process([NotNull] Stream stream, [CanBeNull] ICollection<IJpegSegmentMetadataReader> readers = null)
        {
            if (readers == null)
                readers = _allReaders;

            // Build the union of segment types desired by all readers
            var segmentTypes = new HashSet<JpegSegmentType>(); //(readers.SelectMany(reader => reader.SegmentTypes));
            foreach (var r in readers) { foreach (var s in r.SegmentTypes) segmentTypes.Add(s); }

            // Read out those segments
            var segments = JpegSegmentReader.ReadSegments(new SequentialStreamReader(stream), segmentTypes);

            // Process them
            return ProcessJpegSegments(readers, new List<JpegSegment>(segments));
        }

        [NotNull]
        public static DirectoryList ProcessJpegSegments([NotNull] IEnumerable<IJpegSegmentMetadataReader> readers, [NotNull] ICollection<JpegSegment> segments)
        {
            var directories = new List<Directory>();

            foreach (var reader in readers)
            {
                var readerSegmentTypes = reader.SegmentTypes;
                //var readerSegments = segments.Where(s => readerSegmentTypes.Contains(s.Type));
                List<JpegSegment> readerSegments = new List<JpegSegment>();
                foreach (var s in segments) if (readerSegmentTypes.Contains(s.Type)) readerSegments.Add(s);
                directories.AddRange(reader.ReadJpegSegments(readerSegments));
            }

            return directories;
        }
    }
}

namespace MetadataExtractor.Formats.Png
{
    /// <author>Drew Noakes https://drewnoakes.com</author>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public sealed class PngColorType
    {
        /// <summary>Each pixel is a greyscale sample.</summary>
        public static readonly PngColorType Greyscale = new PngColorType(0, "Greyscale", 1, 2, 4, 8, 16);

        /// <summary>Each pixel is an R,G,B triple.</summary>
        public static readonly PngColorType TrueColor = new PngColorType(2, "True Color", 8, 16);

        /// <summary>Each pixel is a palette index.</summary>
        /// <remarks>Each pixel is a palette index. Seeing this value indicates that a <c>PLTE</c> chunk shall appear.</remarks>
        public static readonly PngColorType IndexedColor = new PngColorType(3, "Indexed Color", 1, 2, 4, 8);

        /// <summary>Each pixel is a greyscale sample followed by an alpha sample.</summary>
        public static readonly PngColorType GreyscaleWithAlpha = new PngColorType(4, "Greyscale with Alpha", 8, 16);

        /// <summary>Each pixel is an R,G,B triple followed by an alpha sample.</summary>
        public static readonly PngColorType TrueColorWithAlpha = new PngColorType(6, "True Color with Alpha", 8, 16);

        [NotNull]
        public static PngColorType FromNumericValue(int numericValue)
        {
            var colorTypes = new[] { Greyscale, TrueColor, IndexedColor, GreyscaleWithAlpha, TrueColorWithAlpha };
            PngColorType res = Array.Find<PngColorType>(colorTypes, (PngColorType colorType) => { return colorType.NumericValue == numericValue; } );
            return res == null ? null
                : new PngColorType(numericValue, "Unknown (" + numericValue + ")");
        }

        public int NumericValue { get { return _NumericValue; } }
        int _NumericValue;

        [NotNull]
        public string Description { get { return _Description; } }
        string _Description;

        [NotNull]
        public int[] AllowedBitDepths { get { return _AllowedBitDepths; } }
        int[] _AllowedBitDepths;

        private PngColorType(int numericValue, [NotNull] string description, [NotNull] params int[] allowedBitDepths)
        {
            _NumericValue = numericValue;
            _Description = description;
            _AllowedBitDepths = allowedBitDepths;
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class PngDescriptor : TagDescriptor<PngDirectory>
    {
        public PngDescriptor([NotNull] PngDirectory directory)
            : base(directory)
        {
        }

        public override string GetDescription(int tagType)
        {
            switch (tagType)
            {
                case PngDirectory.TagColorType:
                    return GetColorTypeDescription();
                case PngDirectory.TagCompressionType:
                    return GetCompressionTypeDescription();
                case PngDirectory.TagFilterMethod:
                    return GetFilterMethodDescription();
                case PngDirectory.TagInterlaceMethod:
                    return GetInterlaceMethodDescription();
                case PngDirectory.TagPaletteHasTransparency:
                    return GetPaletteHasTransparencyDescription();
                case PngDirectory.TagSrgbRenderingIntent:
                    return GetIsSrgbColorSpaceDescription();
                case PngDirectory.TagTextualData:
                    return GetTextualDataDescription();
                case PngDirectory.TagBackgroundColor:
                    return GetBackgroundColorDescription();
                case PngDirectory.TagUnitSpecifier:
                    return GetUnitSpecifierDescription();
                case PngDirectory.TagLastModificationTime:
                    return GetLastModificationTimeDescription();
                default:
                    return base.GetDescription(tagType);
            }
        }

        [CanBeNull]
        public string GetColorTypeDescription()
        {
            int value;
            if (!Directory.TryGetInt32(PngDirectory.TagColorType, out value))
                return null;
            return PngColorType.FromNumericValue(value).Description;
        }

        [CanBeNull]
        public string GetCompressionTypeDescription()
        {
            return GetIndexedDescription(PngDirectory.TagCompressionType, "Deflate");
        }

        [CanBeNull]
        public string GetFilterMethodDescription()
        {
            return GetIndexedDescription(PngDirectory.TagFilterMethod, "Adaptive");
        }

        [CanBeNull]
        public string GetInterlaceMethodDescription()
        {
            return GetIndexedDescription(PngDirectory.TagInterlaceMethod, "No Interlace", "Adam7 Interlace");
        }

        [CanBeNull]
        public string GetPaletteHasTransparencyDescription()
        {
            return GetIndexedDescription(PngDirectory.TagPaletteHasTransparency, null, "Yes");
        }

        [CanBeNull]
        public string GetIsSrgbColorSpaceDescription()
        {
            return GetIndexedDescription(PngDirectory.TagSrgbRenderingIntent, "Perceptual", "Relative Colorimetric", "Saturation", "Absolute Colorimetric");
        }

        [CanBeNull]
        public string GetUnitSpecifierDescription()
        {
            return GetIndexedDescription(PngDirectory.TagUnitSpecifier, "Unspecified", "Metres");
        }

        [CanBeNull]
        public string GetLastModificationTimeDescription()
        {
            DateTime value;
            if (!Directory.TryGetDateTime(PngDirectory.TagLastModificationTime, out value))
                return null;

            return value.ToString("yyyy:MM:dd HH:mm:ss");
        }

        [CanBeNull]
        public string GetTextualDataDescription()
        {
            var pairs = Directory.GetObject(PngDirectory.TagTextualData) as IList<KeyValuePair>;
            string res = null;
            if (pairs != null)
                foreach (KeyValuePair kv in pairs)
                    res = (res == null ? string.Empty : res + "\n") + kv.Key + ": " + kv.Value;
            return res;
        }

        [CanBeNull]
        public string GetBackgroundColorDescription()
        {
            var bytes = Directory.GetByteArray(PngDirectory.TagBackgroundColor);
            int colorType;
            if (bytes == null || !Directory.TryGetInt32(PngDirectory.TagColorType, out colorType))
                return null;

            var reader = new SequentialByteArrayReader(bytes);
            try
            {
                switch (colorType)
                {
                    case 0:
                    case 4:
                        // TODO do we need to normalise these based upon the bit depth?
                        return "Greyscale Level " + reader.GetUInt16();
                    case 2:
                    case 6:
                        return "R " + reader.GetUInt16() + ", G " + reader.GetUInt16() + ", B " + reader.GetUInt16();
                    case 3:
                        return "Palette Index " + reader.GetByte();
                }
            }
            catch (IOException)
            {
                return null;
            }

            return null;
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class PngDirectory : Directory
    {
        public const int TagImageWidth = 1;
        public const int TagImageHeight = 2;
        public const int TagBitsPerSample = 3;
        public const int TagColorType = 4;
        public const int TagCompressionType = 5;
        public const int TagFilterMethod = 6;
        public const int TagInterlaceMethod = 7;
        public const int TagPaletteSize = 8;
        public const int TagPaletteHasTransparency = 9;
        public const int TagSrgbRenderingIntent = 10;
        public const int TagGamma = 11;
        public const int TagIccProfileName = 12;
        public const int TagTextualData = 13;
        public const int TagLastModificationTime = 14;
        public const int TagBackgroundColor = 15;
        public const int TagPixelsPerUnitX = 16;
        public const int TagPixelsPerUnitY = 17;
        public const int TagUnitSpecifier = 18;
        public const int TagSignificantBits = 19;

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagImageHeight, "Image Height" },
            { TagImageWidth, "Image Width" },
            { TagBitsPerSample, "Bits Per Sample" },
            { TagColorType, "Color Type" },
            { TagCompressionType, "Compression Type" },
            { TagFilterMethod, "Filter Method" },
            { TagInterlaceMethod, "Interlace Method" },
            { TagPaletteSize, "Palette Size" },
            { TagPaletteHasTransparency, "Palette Has Transparency" },
            { TagSrgbRenderingIntent, "sRGB Rendering Intent" },
            { TagGamma, "Image Gamma" },
            { TagIccProfileName, "ICC Profile Name" },
            { TagTextualData, "Textual Data" },
            { TagLastModificationTime, "Last Modification Time" },
            { TagBackgroundColor, "Background Color" },
            { TagPixelsPerUnitX, "Pixels Per Unit X" },
            { TagPixelsPerUnitY, "Pixels Per Unit Y" },
            { TagUnitSpecifier, "Unit Specifier" },
            { TagSignificantBits, "Significant Bits" }
        };

        private readonly PngChunkType _pngChunkType;

        public PngDirectory([NotNull] PngChunkType pngChunkType)
        {
            _pngChunkType = pngChunkType;
            SetDescriptor(new PngDescriptor(this));
        }

        [NotNull]
        public PngChunkType GetPngChunkType()
        {
            return _pngChunkType;
        }

        public override string Name { get { return "PNG-" + _pngChunkType.Identifier; } }

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class PngChunkType
    {
        private static readonly ICollection<string> IdentifiersAllowingMultiples
            = new HashSet<string> { "IDAT", "sPLT", "iTXt", "tEXt", "zTXt" };

        #region Standard critical chunks

        /// <summary>
        /// Denotes a critical <see cref="PngChunk"/> that contains basic information about the PNG image.
        /// </summary>
        /// <remarks>
        /// This must be the first chunk in the data sequence, and may only occur once.
        /// <para />
        /// The format is:
        /// <list type="bullet">
        ///   <item><b>pixel width</b> 4 bytes, unsigned and greater than zero</item>
        ///   <item><b>pixel height</b> 4 bytes, unsigned and greater than zero</item>
        ///   <item><b>bit depth</b> 1 byte, number of bits per sample or per palette index (not per pixel)</item>
        ///   <item><b>color type</b> 1 byte, maps to <see cref="PngColorType"/> enum</item>
        ///   <item><b>compression method</b> 1 byte, currently only a value of zero (deflate/inflate) is in the standard</item>
        ///   <item><b>filter method</b> 1 byte, currently only a value of zero (adaptive filtering with five basic filter types) is in the standard</item>
        ///   <item><b>interlace method</b> 1 byte, indicates the transmission order of image data, currently only 0 (no interlace) and 1 (Adam7 interlace) are in the standard</item>
        /// </list>
        /// </remarks>
        public static readonly PngChunkType IHDR = new PngChunkType("IHDR");

        /// <summary>
        /// Denotes a critical <see cref="PngChunk"/> that contains palette entries.
        /// </summary>
        /// <remarks>
        /// This chunk should only appear for a <see cref="PngColorType"/> of <see cref="PngColorType.IndexedColor"/>,
        /// and may only occur once in the PNG data sequence.
        /// <para />
        /// The chunk contains between one and 256 entries, each of three bytes:
        /// <list type="bullet">
        ///   <item><b>red</b> 1 byte</item>
        ///   <item><b>green</b> 1 byte</item>
        ///   <item><b>blue</b> 1 byte</item>
        /// </list>
        /// The number of entries is determined by the chunk length. A chunk length indivisible by three is an error.
        /// </remarks>
        public static readonly PngChunkType PLTE = new PngChunkType("PLTE");

        public static readonly PngChunkType IDAT = new PngChunkType("IDAT", true);

        public static readonly PngChunkType IEND = new PngChunkType("IEND");

        #endregion

        #region Standard ancillary chunks

        public static readonly PngChunkType cHRM = new PngChunkType("cHRM");

        public static readonly PngChunkType gAMA = new PngChunkType("gAMA");

        public static readonly PngChunkType iCCP = new PngChunkType("iCCP");

        public static readonly PngChunkType sBIT = new PngChunkType("sBIT");

        public static readonly PngChunkType sRGB = new PngChunkType("sRGB");

        public static readonly PngChunkType bKGD = new PngChunkType("bKGD");

        public static readonly PngChunkType hIST = new PngChunkType("hIST");

        public static readonly PngChunkType tRNS = new PngChunkType("tRNS");

        public static readonly PngChunkType pHYs = new PngChunkType("pHYs");

        public static readonly PngChunkType sPLT = new PngChunkType("sPLT", true);

        public static readonly PngChunkType tIME = new PngChunkType("tIME");

        public static readonly PngChunkType iTXt = new PngChunkType("iTXt", true);

        /// <summary>
        /// Denotes an ancillary <see cref="PngChunk"/> that contains textual data, having first a keyword and then a value.
        /// </summary>
        /// <remarks>
        /// If multiple text data keywords are needed, then multiple chunks are included in the PNG data stream.
        /// <para />
        /// The format is:
        /// <list type="bullet">
        /// <item><b>keyword</b> 1-79 bytes</item>
        /// <item><b>null separator</b> 1 byte (\0)</item>
        /// <item><b>text string</b> 0 or more bytes</item>
        /// </list>
        /// Text is interpreted according to the Latin-1 character set [ISO-8859-1].
        /// Newlines should be represented by a single linefeed character (0x9).
        /// </remarks>
        public static readonly PngChunkType tEXt = new PngChunkType("tEXt", true);

        public static readonly PngChunkType zTXt = new PngChunkType("zTXt", true);

        #endregion

        [NotNull] private readonly byte[] _bytes;

        public PngChunkType([NotNull] string identifier, bool multipleAllowed = false)
        {
            _AreMultipleAllowed = multipleAllowed;
            var bytes = Encoding.UTF8.GetBytes(identifier);
            ValidateBytes(bytes);
            _bytes = bytes;
        }

        public PngChunkType([NotNull] byte[] bytes)
        {
            ValidateBytes(bytes);
            _bytes = bytes;
            _AreMultipleAllowed = IdentifiersAllowingMultiples.Contains(Identifier);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static void ValidateBytes([NotNull] byte[] bytes)
        {
            if (bytes.Length != 4)
                throw new ArgumentException("PNG chunk type identifier must be four bytes in length");
            foreach (byte b in bytes)
                if (!IsValidByte(b))
                    throw new ArgumentException("PNG chunk type identifier may only contain alphabet characters");
        }

        public bool IsCritical { get { return IsUpperCase(_bytes[0]); } }

        public bool IsAncillary { get { return !IsCritical; } }

        public bool IsPrivate { get { return IsUpperCase(_bytes[1]); } }

        public bool IsSafeToCopy { get { return IsLowerCase(_bytes[3]); } }

        public bool AreMultipleAllowed { get { return _AreMultipleAllowed; } }
        bool _AreMultipleAllowed;

        [Pure]
        private static bool IsLowerCase(byte b) { return (b & (1 << 5)) != 0; }

        [Pure]
        private static bool IsUpperCase(byte b) { return (b & (1 << 5)) == 0; }

        [Pure]
        private static bool IsValidByte(byte b) { return (b >= 65 && b <= 90) || (b >= 97 && b <= 122); }

        [NotNull]
        public string Identifier { get { return Encoding.UTF8.GetString(_bytes, 0, _bytes.Length); } }

        public override string ToString() { return Identifier; }

        #region Equality and Hashing

        private bool Equals([NotNull] PngChunkType other)
        {
            //return _bytes.SequenceEqual(other._bytes);
            if (_bytes.Length != other._bytes.Length) return false;
            for (int i = 0; i != _bytes.Length; i++)
                if (_bytes[i] != other._bytes[i])
                    return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is PngChunkType && Equals((PngChunkType)obj);
        }

        public override int GetHashCode() { return _bytes[0] << 24 | _bytes[1] << 16 << _bytes[2] << 8 | _bytes[3]; }

        public static bool operator ==(PngChunkType left, PngChunkType right) { return Equals(left, right); }
        public static bool operator !=(PngChunkType left, PngChunkType right) { return !Equals(left, right); }

        #endregion
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class PngChunk
    {
        public PngChunk([NotNull] PngChunkType chunkType, [NotNull] byte[] bytes)
        {
            _ChunkType = chunkType;
            _Bytes = bytes;
        }

        [NotNull]
        public PngChunkType ChunkType { get { return _ChunkType; } }
        PngChunkType _ChunkType;

        [NotNull]
        public byte[] Bytes { get { return _Bytes; } }
        byte[] _Bytes;
    }

    /// <summary>An exception class thrown upon unexpected and fatal conditions while processing a JPEG file.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class PngProcessingException : ImageProcessingException
    {
        public PngProcessingException([CanBeNull] string message)
            : base(message)
        {
        }

        public PngProcessingException([CanBeNull] string message, [CanBeNull] Exception innerException)
            : base(message, innerException)
        {
        }

        public PngProcessingException([CanBeNull] Exception innerException)
            : base(innerException)
        {
        }

#if !NETSTANDARD1_3
        protected PngProcessingException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class PngChunkReader
    {
        private static readonly byte[] _pngSignatureBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <exception cref="PngProcessingException"/>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public IEnumerable<PngChunk> Extract([NotNull] SequentialReader reader, [CanBeNull] ICollection<PngChunkType> desiredChunkTypes)
        {
            //
            // PNG DATA STREAM
            //
            // Starts with a PNG SIGNATURE, followed by a sequence of CHUNKS.
            //
            // PNG SIGNATURE
            //
            //   Always composed of these bytes: 89 50 4E 47 0D 0A 1A 0A
            //
            // CHUNK
            //
            //   4 - length of the data field (unsigned, but always within 31 bytes), may be zero
            //   4 - chunk type, restricted to [65,90] and [97,122] (A-Za-z)
            //   * - data field
            //   4 - CRC calculated from chunk type and chunk data, but not length
            //
            // CHUNK TYPES
            //
            //   Critical Chunk Types:
            //
            //     IHDR - image header, always the first chunk in the data stream
            //     PLTE - palette table, associated with indexed PNG images
            //     IDAT - image data chunk, of which there may be many
            //     IEND - image trailer, always the last chunk in the data stream
            //
            //   Ancillary Chunk Types:
            //
            //     Transparency information:  tRNS
            //     Colour space information:  cHRM, gAMA, iCCP, sBIT, sRGB
            //     Textual information:       iTXt, tEXt, zTXt
            //     Miscellaneous information: bKGD, hIST, pHYs, sPLT
            //     Time information:          tIME
            //

            // network byte order
            reader = reader.WithByteOrder(isMotorolaByteOrder: true);

            byte[] readSignature = reader.GetBytes(_pngSignatureBytes.Length);
            for (int i = 0; i != _pngSignatureBytes.Length; i++)
                if (_pngSignatureBytes[i] != readSignature[i])
                    throw new PngProcessingException("PNG signature mismatch");

            var seenImageHeader = false;
            var seenImageTrailer = false;
            var chunks = new List<PngChunk>();
            var seenChunkTypes = new HashSet<PngChunkType>();

            while (!seenImageTrailer)
            {
                // Process the next chunk.
                var chunkDataLength = reader.GetInt32();
                if (chunkDataLength < 0)
                    throw new PngProcessingException("PNG chunk length exceeds maximum");
                var chunkType = new PngChunkType(reader.GetBytes(4));
                var willStoreChunk = desiredChunkTypes == null || desiredChunkTypes.Contains(chunkType);
                var chunkData = reader.GetBytes(chunkDataLength);

                // Skip the CRC bytes at the end of the chunk
                // TODO consider verifying the CRC value to determine if we're processing bad data
                reader.Skip(4);

                if (willStoreChunk && seenChunkTypes.Contains(chunkType) && !chunkType.AreMultipleAllowed)
                    throw new PngProcessingException("Observed multiple instances of PNG chunk '" + chunkType + "', for which multiples are not allowed");

                if (chunkType.Equals(PngChunkType.IHDR))
                    seenImageHeader = true;
                else if (!seenImageHeader)
                    throw new PngProcessingException("First chunk should be '" + PngChunkType.IHDR + "', but '" + chunkType + "' was observed");

                if (chunkType.Equals(PngChunkType.IEND))
                    seenImageTrailer = true;

                if (willStoreChunk)
                    chunks.Add(new PngChunk(chunkType, chunkData));

                seenChunkTypes.Add(chunkType);
            }

            return chunks;
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class PngHeader
    {
        /// <exception cref="PngProcessingException"/>
        public PngHeader([NotNull] byte[] bytes)
        {
            if (bytes.Length != 13)
                throw new PngProcessingException("PNG header chunk must have exactly 13 data bytes");

            var reader = new SequentialByteArrayReader(bytes);

            _ImageWidth = reader.GetInt32();
            _ImageHeight = reader.GetInt32();
            _BitsPerSample = reader.GetByte();
            _ColorType = PngColorType.FromNumericValue(reader.GetByte());
            _CompressionType = reader.GetByte();
            _FilterMethod = reader.GetByte();
            _InterlaceMethod = reader.GetByte();
        }

        public int ImageWidth { get { return _ImageWidth; } }
        int _ImageWidth;

        public int ImageHeight { get { return _ImageHeight; } }
        int _ImageHeight;

        public byte BitsPerSample { get { return _BitsPerSample; } }
        byte _BitsPerSample;

        [NotNull]
        public PngColorType ColorType { get { return _ColorType; } }
        PngColorType _ColorType;

        public byte CompressionType { get { return _CompressionType; } }
        byte _CompressionType;

        public byte FilterMethod { get { return _FilterMethod; } }
        byte _FilterMethod;

        public byte InterlaceMethod { get { return _InterlaceMethod; } }
        byte _InterlaceMethod;
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class PngChromaticities
    {
        public int WhitePointX { get { return _WhitePointX; } }
        int _WhitePointX;
        public int WhitePointY { get { return _WhitePointY; } }
        int _WhitePointY;
        public int RedX { get { return _RedX; } }
        int _RedX;
        public int RedY { get { return _RedY; } }
        int _RedY;
        public int GreenX { get { return _GreenX; } }
        int _GreenX;
        public int GreenY { get { return _GreenY; } }
        int _GreenY;
        public int BlueX { get { return _BlueX; } }
        int _BlueX;
        public int BlueY { get { return _BlueY; } }
        int _BlueY;

        /// <exception cref="PngProcessingException"/>
        public PngChromaticities([NotNull] byte[] bytes)
        {
            if (bytes.Length != 8*4)
                throw new PngProcessingException("Invalid number of bytes");

            var reader = new SequentialByteArrayReader(bytes);

            try
            {
                _WhitePointX = reader.GetInt32();
                _WhitePointY = reader.GetInt32();
                _RedX = reader.GetInt32();
                _RedY = reader.GetInt32();
                _GreenX = reader.GetInt32();
                _GreenY = reader.GetInt32();
                _BlueX = reader.GetInt32();
                _BlueY = reader.GetInt32();
            }
            catch (IOException ex)
            {
                throw new PngProcessingException(ex);
            }
        }
    }

    /// <author>Drew Noakes https://drewnoakes.com</author>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class PngChromaticitiesDirectory : Directory
    {
        public const int TagWhitePointX = 1;
        public const int TagWhitePointY = 2;
        public const int TagRedX = 3;
        public const int TagRedY = 4;
        public const int TagGreenX = 5;
        public const int TagGreenY = 6;
        public const int TagBlueX = 7;
        public const int TagBlueY = 8;

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>
        {
            { TagWhitePointX, "White Point X" },
            { TagWhitePointY, "White Point Y" },
            { TagRedX, "Red X" },
            { TagRedY, "Red Y" },
            { TagGreenX, "Green X" },
            { TagGreenY, "Green Y" },
            { TagBlueX, "Blue X" },
            { TagBlueY, "Blue Y" }
        };

        public PngChromaticitiesDirectory()
        {
            SetDescriptor(new TagDescriptor<PngChromaticitiesDirectory>(this));
        }

        public override string Name { get { return "PNG Chromaticities"; } }

        protected override bool TryGetTagName(int tagType, out string tagName) { return _tagNameMap.TryGetValue(tagType, out tagName); }
    }
}

namespace MetadataExtractor.Formats.Png
{
    using System.IO.Compression;

    /// <author>Drew Noakes https://drewnoakes.com</author>
    public static class PngMetadataReader
    {
        private static readonly HashSet<PngChunkType> _desiredChunkTypes = new HashSet<PngChunkType>
        {
            PngChunkType.IHDR,
            PngChunkType.PLTE,
            PngChunkType.tRNS,
            PngChunkType.cHRM,
            PngChunkType.sRGB,
            PngChunkType.gAMA,
            PngChunkType.iCCP,
            PngChunkType.bKGD,
            PngChunkType.tEXt,
            PngChunkType.zTXt,
            PngChunkType.iTXt,
            PngChunkType.tIME,
            PngChunkType.pHYs,
            PngChunkType.sBIT
        };

        /// <exception cref="PngProcessingException"/>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] string filePath)
        {
            var directories = new List<Directory>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                directories.AddRange(ReadMetadata(stream));

#if METADATAEXTRACTOR_HAVE_FILEMETADATA
            directories.Add(new FileMetadataReader().Read(filePath));
#endif

            return directories;
        }

        /// <exception cref="PngProcessingException"/>
        /// <exception cref="System.IO.IOException"/>
        [NotNull]
        public static DirectoryList ReadMetadata([NotNull] Stream stream)
        {
            //return new PngChunkReader()
            //    .Extract(new SequentialStreamReader(stream), _desiredChunkTypes)
            //    .SelectMany(ProcessChunk)
            //    .ToList();
            List<Directory> res = new List<Directory>();
            foreach (PngChunk c in new PngChunkReader().Extract(new SequentialStreamReader(stream), _desiredChunkTypes))
                res.AddRange(ProcessChunk(c));
            return res;
        }

        /// <summary>
        /// The PNG spec states that ISO_8859_1 (Latin-1) encoding should be used for:
        /// <list type="bullet">
        ///   <item>"tEXt" and "zTXt" chunks, both for keys and values (https://www.w3.org/TR/PNG/#11tEXt)</item>
        ///   <item>"iCCP" chunks, for the profile name (https://www.w3.org/TR/PNG/#11iCCP)</item>
        ///   <item>"sPLT" chunks, for the palette name (https://www.w3.org/TR/PNG/#11sPLT)</item>
        /// </list>
        /// Note that "iTXt" chunks use UTF-8 encoding (https://www.w3.org/TR/PNG/#11iTXt).
        /// <para/>
        /// For more guidance: http://www.w3.org/TR/PNG-Decoders.html#D.Text-chunk-processing
        /// </summary>
        private static readonly Encoding _latin1Encoding = Encoding.GetEncoding("ISO-8859-1");

        /// <exception cref="PngProcessingException"/>
        /// <exception cref="System.IO.IOException"/>
        private static IEnumerable<Directory> ProcessChunk([NotNull] PngChunk chunk)
        {
            var chunkType = chunk.ChunkType;
            var bytes = chunk.Bytes;

            if (chunkType == PngChunkType.IHDR)
            {
                var header = new PngHeader(bytes);
                var directory = new PngDirectory(PngChunkType.IHDR);
                directory.Set(PngDirectory.TagImageWidth, header.ImageWidth);
                directory.Set(PngDirectory.TagImageHeight, header.ImageHeight);
                directory.Set(PngDirectory.TagBitsPerSample, header.BitsPerSample);
                directory.Set(PngDirectory.TagColorType, header.ColorType.NumericValue);
                directory.Set(PngDirectory.TagCompressionType, header.CompressionType);
                directory.Set(PngDirectory.TagFilterMethod, header.FilterMethod);
                directory.Set(PngDirectory.TagInterlaceMethod, header.InterlaceMethod);
                yield return directory;
            }
            else if (chunkType == PngChunkType.PLTE)
            {
                var directory = new PngDirectory(PngChunkType.PLTE);
                directory.Set(PngDirectory.TagPaletteSize, bytes.Length / 3);
                yield return directory;
            }
            else if (chunkType == PngChunkType.tRNS)
            {
                var directory = new PngDirectory(PngChunkType.tRNS);
                directory.Set(PngDirectory.TagPaletteHasTransparency, 1);
                yield return directory;
            }
            else if (chunkType == PngChunkType.sRGB)
            {
                int srgbRenderingIntent = unchecked((sbyte)bytes[0]);
                var directory = new PngDirectory(PngChunkType.sRGB);
                directory.Set(PngDirectory.TagSrgbRenderingIntent, srgbRenderingIntent);
                yield return directory;
            }
            else if (chunkType == PngChunkType.cHRM)
            {
                var chromaticities = new PngChromaticities(bytes);
                var directory = new PngChromaticitiesDirectory();
                directory.Set(PngChromaticitiesDirectory.TagWhitePointX, chromaticities.WhitePointX);
                directory.Set(PngChromaticitiesDirectory.TagWhitePointY, chromaticities.WhitePointY);
                directory.Set(PngChromaticitiesDirectory.TagRedX, chromaticities.RedX);
                directory.Set(PngChromaticitiesDirectory.TagRedY, chromaticities.RedY);
                directory.Set(PngChromaticitiesDirectory.TagGreenX, chromaticities.GreenX);
                directory.Set(PngChromaticitiesDirectory.TagGreenY, chromaticities.GreenY);
                directory.Set(PngChromaticitiesDirectory.TagBlueX, chromaticities.BlueX);
                directory.Set(PngChromaticitiesDirectory.TagBlueY, chromaticities.BlueY);
                yield return directory;
            }
            else if (chunkType == PngChunkType.gAMA)
            {
                var gammaInt = ByteConvert.ToInt32BigEndian(bytes);
                var directory = new PngDirectory(PngChunkType.gAMA);
                directory.Set(PngDirectory.TagGamma, gammaInt / 100000.0);
                yield return directory;
            }
#if METADATAEXTRACTOR_HAVE_UNDATED_SUPPORT
            else if (chunkType == PngChunkType.iCCP)
            {
                var reader = new SequentialByteArrayReader(bytes);
                var profileName = reader.GetNullTerminatedStringValue(maxLengthBytes: 79);
                var directory = new PngDirectory(PngChunkType.iCCP);
                directory.Set(PngDirectory.TagIccProfileName, profileName);
                var compressionMethod = reader.GetSByte();
                if (compressionMethod == 0)
                {
                    // Only compression method allowed by the spec is zero: deflate
                    // This assumes 1-byte-per-char, which it is by spec.
                    var bytesLeft = bytes.Length - profileName.Bytes.Length - 2;

                    // http://george.chiramattel.com/blog/2007/09/deflatestream-block-length-does-not-match.html
                    // First two bytes are part of the zlib specification (RFC 1950), not the deflate specification (RFC 1951).
                    reader.GetByte(); reader.GetByte();
                    bytesLeft -= 2;

                    var compressedProfile = reader.GetBytes(bytesLeft);
                    using (var inflaterStream = new DeflateStream(new MemoryStream(compressedProfile), CompressionMode.Decompress))
                    {
                        var iccDirectory = new IccReader().Extract(new IndexedCapturingReader(inflaterStream));
                        iccDirectory.Parent = directory;
                        yield return iccDirectory;
                    }
                }
                else
                {
                    directory.AddError("Invalid compression method value");
                }
                yield return directory;
            }
#endif
            else if (chunkType == PngChunkType.bKGD)
            {
                var directory = new PngDirectory(PngChunkType.bKGD);
                directory.Set(PngDirectory.TagBackgroundColor, bytes);
                yield return directory;
            }
            else if (chunkType == PngChunkType.tEXt)
            {
                var reader = new SequentialByteArrayReader(bytes);
                var keyword = reader.GetNullTerminatedStringValue(maxLengthBytes: 79).ToString(_latin1Encoding);
                var bytesLeft = bytes.Length - keyword.Length - 1;
                var value = reader.GetNullTerminatedStringValue(bytesLeft, _latin1Encoding);

                var textPairs = new List<KeyValuePair> { new KeyValuePair(keyword, value) };
                var directory = new PngDirectory(PngChunkType.tEXt);
                directory.Set(PngDirectory.TagTextualData, textPairs);
                yield return directory;
            }
            else if (chunkType == PngChunkType.zTXt)
            {
                var reader = new SequentialByteArrayReader(bytes);
                var keyword = reader.GetNullTerminatedStringValue(maxLengthBytes: 79).ToString(_latin1Encoding);
                var compressionMethod = reader.GetSByte();

                var bytesLeft = bytes.Length - keyword.Length - 1 - 1 - 1 - 1;
                byte[] textBytes = null;
                if (compressionMethod == 0)
                {
                    using (var inflaterStream = new DeflateStream(new MemoryStream(bytes, bytes.Length - bytesLeft, bytesLeft), CompressionMode.Decompress))
                    {
                        Exception ex = null;
                        try
                        {
                            textBytes = ReadStreamToBytes(inflaterStream);
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }

                        // Work-around no yield-return from catch blocks
                        if (ex != null)
                        {
                            var directory = new PngDirectory(PngChunkType.zTXt);
                            directory.AddError("Exception decompressing " + "PngChunkType.zTXt" + " chunk with keyword \"" + keyword + "\": " + ex.Message);
                            yield return directory;
                        }
                    }
                }
                else
                {
                    var directory = new PngDirectory(PngChunkType.zTXt);
                    directory.AddError("Invalid compression method value");
                    yield return directory;
                }
                if (textBytes != null)
                {
#if METADATAEXTRACTOR_HAVE_XMP_SUPPORT
                    if (keyword == "XML:com.adobe.xmp")
                    {
                        // NOTE in testing images, the XMP has parsed successfully, but we are not extracting tags from it as necessary
                        yield return new XmpReader().Extract(textBytes);
                    }
                    else
#endif
                    {
                        var textPairs = new List<KeyValuePair> { new KeyValuePair(keyword, new StringValue(textBytes, _latin1Encoding)) };
                        var directory = new PngDirectory(PngChunkType.zTXt);
                        directory.Set(PngDirectory.TagTextualData, textPairs);
                        yield return directory;
                    }
                }
            }
            else if (chunkType == PngChunkType.iTXt)
            {
                var reader = new SequentialByteArrayReader(bytes);
                var keyword = reader.GetNullTerminatedStringValue(maxLengthBytes: 79).ToString(_latin1Encoding);
                var compressionFlag = reader.GetSByte();
                var compressionMethod = reader.GetSByte();

                // TODO we currently ignore languageTagBytes and translatedKeywordBytes
                var languageTagBytes = reader.GetNullTerminatedBytes(bytes.Length);
                var translatedKeywordBytes = reader.GetNullTerminatedBytes(bytes.Length);

                var bytesLeft = bytes.Length - keyword.Length - 1 - 1 - 1 - languageTagBytes.Length - 1 - translatedKeywordBytes.Length - 1;
                byte[] textBytes = null;
                if (compressionFlag == 0)
                {
                    textBytes = reader.GetNullTerminatedBytes(bytesLeft);
                }
                else if (compressionFlag == 1)
                {
                    if (compressionMethod == 0)
                    {
                        using (var inflaterStream = new DeflateStream(new MemoryStream(bytes, bytes.Length - bytesLeft, bytesLeft), CompressionMode.Decompress))
                        {
                            Exception ex = null;
                            try
                            {
                                textBytes = ReadStreamToBytes(inflaterStream);
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }

                            // Work-around no yield-return from catch blocks
                            if (ex != null)
                            {
                                var directory = new PngDirectory(PngChunkType.iTXt);
                                directory.AddError("Exception decompressing " + "PngChunkType.iTXt" + " chunk with keyword \"" + keyword + "\": " + ex.Message);
                                yield return directory;
                            }
                        }
                    }
                    else
                    {
                        var directory = new PngDirectory(PngChunkType.iTXt);
                        directory.AddError("Invalid compression method value");
                        yield return directory;
                    }
                }
                else
                {
                    var directory = new PngDirectory(PngChunkType.iTXt);
                    directory.AddError("Invalid compression flag value");
                    yield return directory;
                }

                if (textBytes != null)
                {
#if METADATAEXTRACTOR_HAVE_XMP_SUPPORT
                    if (keyword == "XML:com.adobe.xmp")
                    {
                        // NOTE in testing images, the XMP has parsed successfully, but we are not extracting tags from it as necessary
                        yield return new XmpReader().Extract(textBytes);
                    }
                    else
#endif
                    {
                        var textPairs = new List<KeyValuePair> { new KeyValuePair(keyword, new StringValue(textBytes, _latin1Encoding)) };
                        var directory = new PngDirectory(PngChunkType.iTXt);
                        directory.Set(PngDirectory.TagTextualData, textPairs);
                        yield return directory;
                    }
                }
            }
            else if (chunkType == PngChunkType.tIME)
            {
                var reader = new SequentialByteArrayReader(bytes);
                var year = reader.GetUInt16();
                var month = reader.GetByte();
                int day = reader.GetByte();
                int hour = reader.GetByte();
                int minute = reader.GetByte();
                int second = reader.GetByte();
                var directory = new PngDirectory(PngChunkType.tIME);
                if (DateUtil.IsValidDate(year, month, day) && DateUtil.IsValidTime(hour, minute, second))
                {
                    var time = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
                    directory.Set(PngDirectory.TagLastModificationTime, time);
                }
                else
                    directory.AddError("PNG tIME data describes an invalid date/time: year=" + year + " month=" + month + " day=" + day + " hour=" + hour + " minute=" + minute + " second=" + second);
                yield return directory;
            }
            else if (chunkType == PngChunkType.pHYs)
            {
                var reader = new SequentialByteArrayReader(bytes);
                var pixelsPerUnitX = reader.GetInt32();
                var pixelsPerUnitY = reader.GetInt32();
                var unitSpecifier = reader.GetSByte();
                var directory = new PngDirectory(PngChunkType.pHYs);
                directory.Set(PngDirectory.TagPixelsPerUnitX, pixelsPerUnitX);
                directory.Set(PngDirectory.TagPixelsPerUnitY, pixelsPerUnitY);
                directory.Set(PngDirectory.TagUnitSpecifier, unitSpecifier);
                yield return directory;
            }
            else if (chunkType.Equals(PngChunkType.sBIT))
            {
                var directory = new PngDirectory(PngChunkType.sBIT);
                directory.Set(PngDirectory.TagSignificantBits, bytes);
                yield return directory;
            }
        }

        private static byte[] ReadStreamToBytes(Stream stream)
        {
            var ms = new MemoryStream();

#if !DOTNET35
            stream.CopyTo(ms);
#else
            var buffer = new byte[1024];
            int count;
            while ((count = stream.Read(buffer, 0, 256)) > 0)
                ms.Write(buffer, 0, count);
#endif

            return ms.ToArray();
        }
    }
}

// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

namespace SharpOnvifCommon.PTZ
{
    public static class SpacesPanTilt
    {
        public const string POSITION_GENERIC_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/PositionGenericSpace";
        public const string POSITION_SPHERICAL_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/SphericalPositionSpace";
        public const string POSITION_DIGITAL_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/DigitalPositionSpace";
        public const string TRANSLATION_GENERIC_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/TranslationGenericSpace";
        public const string TRANSLATION_SPHERICAL_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/SphericalTranslation";
        public const string TRANSLATION_FOV_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/TranslationSpaceFov";
        public const string TRANSLATION_DIGITAL_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/DigitalTranslationSpace";
        public const string VELOCITY_GENERIC_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/VelocityGenericSpace";
        public const string VELOCITY_DEGREES_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/VelocitySpaceDegrees";
        public const string VELOCITY_FOV_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/VelocitySpaceFOV";
        public const string SPEED_GENERIC_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/GenericSpeedSpace";
        public const string SPEED_DEGREES_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/SpeedSpaceDegrees";
        public const string SPEED_FOV_SPACE = "http://www.onvif.org/ver10/tptz/PanTiltSpaces/SpeedSpaceFOV";
    }

    public static class SpacesZoom
    {
        public const string POSITION_GENERIC_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/PositionGenericSpace";
        public const string POSITION_MILLIMETER_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/PositionSpaceMillimeter";
        public const string POSITION_DIGITAL_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/NormalizedDigitalPosition";
        public const string TRANSLATION_GENERIC_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/TranslationGenericSpace";
        public const string TRANSLATION_MILLIMETER_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/TranslationSpaceMillimeter";
        public const string TRANSLATION_DIGITAL_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/NormalizedDigital";
        public const string VELOCITY_MILLIMETER_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/VelocitySpaceMillimeter";
        public const string VELOCITY_GENERIC_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/VelocityGenericSpace";
        public const string VELOCITY_DIGITAL_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/NormalizedDigitalVelocity";
        public const string SPEED_GENERIC_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/ZoomGenericSpeedSpace";
        public const string SPEED_MILLIMETER_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/SpeedSpaceMillimeter";
        public const string SPEED_DIGITAL_SPACE = "http://www.onvif.org/ver10/tptz/ZoomSpaces/NormalizedDigitalSpeedSpace";
    }
}

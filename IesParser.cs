using System;
using System.Collections.Generic;
using UnityEngine;

public class IesConfig
{
    public enum TiltInfoType
    {
        NONE,
        INCLUDE,
        FILE
    }

    // Tilt Information
    public class TiltInfo
    {
        public int lampToLuminaireGeometry;
        public int numOfTiltAngles { get; protected set; }
        public double[] angles { get { return m_angles; } }
        protected double[] m_angles;

        public double[] multiplyingFactors { get { return m_multiplyingFactors; } }
        protected double[] m_multiplyingFactors;

        public void SetNumberOfTiltAngles(int value)
        {
            if (value < 1)
                return;

            numOfTiltAngles = value;
            m_angles = new double[numOfTiltAngles];
            m_multiplyingFactors = new double[numOfTiltAngles];
        }

        public void SetAngle(int index, double value)
        {
            if (index < 0 || index > numOfTiltAngles)
                return;
            if (m_angles == null)
                m_angles = new double[numOfTiltAngles];

            m_angles[numOfTiltAngles] = value;
        }

        public void SetMultiplyingFactor(int index, double value)
        {
            if (index < 0 || index > numOfTiltAngles)
                return;
            if (m_multiplyingFactors == null)
                m_multiplyingFactors = new double[numOfTiltAngles];

            m_multiplyingFactors[numOfTiltAngles] = value;
        }
    }

    // header of the file.
    public string formatInfo { get; protected set; }

    // Keyword - value.
    protected Dictionary<string/* keywords */, string/* value */> m_keywordValue;

    // TILT type of the light source.
    public TiltInfoType tiltType { get; protected set; }
    // TILT infomation (if TILT=INCLUDE)
    public TiltInfo includedTiltInfo { get; protected set; }
    // TILT information file name (if TILT = <fileName>)
    public string tiltFileName { get; protected set; }

    // value data
    public int numberOfLamps = 1;
    public double lumenPerLamp = 0;
    public double candelaMultiplier = 1;
    public int numberOfVerticalAngles = 1;
    public int numberOfHorizontalAngles = 1;
    public int photometricType = 1;
    public int unitsType = 2;
    public double width = 0;
    public double length = 0;
    public double height = 0;
    public double ballastFactor = 1;
    public double futureUse = 1;
    public double inputWatts = 0;

    // Angle - Intensity data
    protected double[] m_verticalAngles = new double[] { 0 };
    protected double[] m_horizontalAngles = new double[] { 0 };
    protected List<double[]> m_rawCandelaIntensity = new List<double[]>() { new double[] { 0 } };

    public IesConfig(string formatInfo)
    {
        this.formatInfo = formatInfo;
    }

    #region keyword

    /// Get / Set a keyword
    public string this[string keyword]
    {
        get{ return m_keywordValue == null ? null : m_keywordValue[keyword];}

        set
        {
            if(m_keywordValue == null)
                m_keywordValue = new Dictionary<string, string>();

            if (m_keywordValue.ContainsKey(keyword))
                m_keywordValue[keyword] = value;
            else
                m_keywordValue.Add(keyword, value);
        }
    }

    /// Remove a keyword
    public void RemoveKeyword(string keyword)
    {
        if (m_keywordValue == null)
            return;

        m_keywordValue.Remove(keyword);
    }

    #endregion

    #region tilt
    public void SetTiltInfo(TiltInfoType tiltType, TiltInfo includedInfo = null,  string tiltFileName = null)
    {
        switch(tiltType)
        {
            case TiltInfoType.NONE:
                break;
            case TiltInfoType.INCLUDE:
                includedTiltInfo = includedInfo;
                break;
            case TiltInfoType.FILE:
                this.tiltFileName = tiltFileName;
                break;
            default:
                return;
        }

        this.tiltType = tiltType;
    }
    #endregion

    #region angleIntensityData

    /// <summary>
    /// Get Intensity with angle index (clampped)
    /// </summary>
    /// <param name="verticalAngleIndex"></param>
    /// <param name="horizontalAngleIndex"></param>
    /// <returns></returns>
    public double this[int verticalAngleIndex, int horizontalAngleIndex]
    {
        get
        {
            int vertIndex = Math.Max(Math.Min(verticalAngleIndex, m_verticalAngles.Length - 1), 0);
            int honrIndex = Math.Max(Math.Min(horizontalAngleIndex, m_horizontalAngles.Length - 1), 0);
            return m_rawCandelaIntensity[honrIndex][vertIndex];
        }
    }

    public void SetRawData(int verticalAngleIndex, int horizontalAngleIndex, double value)
    {
        if (verticalAngleIndex < 0 || verticalAngleIndex >= m_verticalAngles.Length)
            throw new Exception("Vertical angle index is out of range.");

        if (horizontalAngleIndex < 0 || horizontalAngleIndex >= m_horizontalAngles.Length)
            throw new Exception("Horizontal angle index is out of range.");

        m_rawCandelaIntensity[horizontalAngleIndex][verticalAngleIndex] = value;
    }

    public double[] VerticalAngles { get { return m_verticalAngles; } }

    public double[] HorizontalAngles { get { return m_horizontalAngles; } }

    public double GetRawIntensityData(int verticalIndex, int horizontalIndex)
    {
        return m_rawCandelaIntensity[horizontalIndex][verticalIndex];
    }

    public void SetVerticalAngles(double[] verticalAngles, bool check = true)
    {
        if (verticalAngles == null || verticalAngles.Length == 0)
            return;

        if(check)
        {
            if (verticalAngles[0] != 0d)
                throw new Exception("The first data of vertical angle should be exactly 0.");

            int length = verticalAngles.Length;

            for(int i = 0; i < (length-1);++i)
            {
                if (verticalAngles[i] >= verticalAngles[i + 1])
                    throw new Exception("The vertical angles should be an ascending order array.");
            }

            // check angle value:
            if(this.photometricType == 1) // Type C light source
            {
                if(verticalAngles[0] == 0d || Math.Abs(verticalAngles[0] - 90d ) <= 0.1d) { }
                else throw new Exception("The first data of vertical angles for Type C should be 0 or 90.");

                if (Math.Abs(verticalAngles[length-1] - 90d) < 0.1d || Math.Abs(verticalAngles[length - 1] - 180d) < 0.1d) { }
                else throw new Exception("The last data of vertical angles for Type C should be 90 or 180.");
            }
            else  // Type A and B light source
            {
                if (verticalAngles[0] == 0d || Math.Abs(verticalAngles[0] + 90d) <= 0.1d) { }
                else throw new Exception("The first data of vertical angles for Type A and B should be 0 or -90.");

                if (Math.Abs(verticalAngles[length - 1] - 90d) < 0.1d ) { }
                else throw new Exception("The last data of vertical angles for Type A and B should be 90 or 180.");
            }
        }

        // TODO: add resize.
        m_verticalAngles = verticalAngles;
    }

    public void SetHorizontalAngles(double[] horizontalAngles, bool check = true)
    {
        if (horizontalAngles == null || horizontalAngles.Length == 0)
            return;

        if (check)
        {
            // TODO: add check.
        }

        // TODO: add resize.
        m_horizontalAngles = horizontalAngles;
    }

    public void SetRawIntensityData(List<double[]> rawData)
    {
        m_rawCandelaIntensity = rawData;
    }
    #endregion
}
public class IesFileParser
{
    const string iesHeaderFlag = "IESNA";
    const string tiltInfoFlag = "TILT";
    const string tiltInfoNoneStr = "NONE";
    const string tiltInfoIncludeStr = "INCLUDE";

    public class ParseSettings
    {
        public static readonly ParseSettings defaultSettings = new ParseSettings();
        public bool ignoreHeaderAndKeyword = false;
    }
    public static IesConfig Parse(string[] lines, ParseSettings setting)
    {
        if(lines == null)
            throw new Exception("Empty input.");

        if (lines.Length < 3)
            throw new Exception("Invalid IES file lines.");

        string formatInfo = lines[0];

        if (!setting.ignoreHeaderAndKeyword)
        {
            // check ies format header.
            if (string.IsNullOrEmpty(formatInfo) || !formatInfo.Contains(iesHeaderFlag))
                throw new Exception("Invalid format header.");
        }

        IesConfig result = new IesConfig(setting.ignoreHeaderAndKeyword ? "" : formatInfo);

        int tiltLineIndex = -1;
        for(int i = 0; i < lines.Length;++i)
        {
            if(lines[i].Contains(tiltInfoFlag))
            {
                tiltLineIndex = i;
                break;
            }
        }

        if (tiltLineIndex == -1)
            throw new Exception("Cannot find TILT information.");

        // Parse Keyword:
        if (!setting.ignoreHeaderAndKeyword)
        {
            for (int i = 1; i < tiltLineIndex; ++i)
            {
                string keywordLine = lines[i];
                int leftSqrBrIndex = keywordLine.IndexOf('[');
                if (leftSqrBrIndex >= 0)
                {
                    int rightSqrBrIndex = keywordLine.IndexOf(']');
                    int keywordLength = rightSqrBrIndex - leftSqrBrIndex - 1;
                    if (keywordLength > 0)
                    {
                        string keyword = keywordLine.Substring(leftSqrBrIndex + 1, keywordLength);
                        string keywordValue = "";
                        int keywordValueLength = keywordLine.Length - rightSqrBrIndex - 1;
                        if (keywordValueLength > 0)
                            keywordValue = keywordLine.Substring(rightSqrBrIndex + 1, keywordValueLength).Trim();

                        result[keyword] = keywordValue;
                    }
                }
            }
        }

        // parse tilt:
        string tiltInfoLine = lines[tiltLineIndex];
        if (string.IsNullOrEmpty(tiltInfoLine))
            throw new Exception("TILT infomation is invalid.");

        string tiltTypeValue = "";
        try
        {
            tiltTypeValue = tiltInfoLine.Substring(tiltInfoLine.IndexOf('=') + 1).Trim();
        }
        catch (Exception)
        {
            tiltTypeValue = "";
        }

        if (tiltTypeValue == tiltInfoNoneStr)
        {
            result.SetTiltInfo(IesConfig.TiltInfoType.NONE);
        }
        else if(tiltTypeValue == tiltInfoIncludeStr)
        {
            // Inlcude tilt information will parse later
            result.SetTiltInfo(IesConfig.TiltInfoType.INCLUDE,null,null);

        }
        else if(tiltTypeValue.Length>0)
        {
            result.SetTiltInfo(IesConfig.TiltInfoType.FILE,null, tiltTypeValue);
        }
        else
        {
            throw new Exception("TILT type is invalide.");
        }

        /// Parse digit values:
        List<string> digitValue = new List<string>();

        // separate strings with ' '
        int digitLine = tiltLineIndex + 1;
        if (digitLine >= lines.Length)
            throw new Exception("Cannot find any digit information.");

        char[] delimiterChars = { ' ','\t'};
        for (int i = digitLine; i < lines.Length;++i)
        {
            string line = lines[i];
            digitValue.AddRange(line.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries));
        }
        int totalValueCount = digitValue.Count;
        int valueIndex = 0;

        // parse tilt data:
        if(result.tiltType == IesConfig.TiltInfoType.INCLUDE)
        {
            IesConfig.TiltInfo tiltInfo = new IesConfig.TiltInfo();

            if (valueIndex + 2 > totalValueCount)
                throw new Exception("Invalid tilt data count.");

            string lampToLuminaireGeometryStr = digitValue[valueIndex];
            valueIndex++;
            string numberOfTiltAnglesStr = digitValue[valueIndex];
            valueIndex++;

            // lampToLuminaireGeometry
            if (!int.TryParse(lampToLuminaireGeometryStr, out tiltInfo.lampToLuminaireGeometry))
                throw new Exception("Parse lampToLuminaireGeometry error");

            // numberOfTiltAngles
            int numberOfTiltAngles = -1;
            if (!int.TryParse(numberOfTiltAnglesStr, out numberOfTiltAngles))
                throw new Exception("Parse numberOfTiltAngles error");

            tiltInfo.SetNumberOfTiltAngles(numberOfTiltAngles);

            // there should be at least numberOfTiltAngles values for <angles>
            if (valueIndex + numberOfTiltAngles > totalValueCount)
                throw new Exception("Invalid tilt angles data count.");

            // following <angles> there should be at least numberOfTiltAngles values for <multiplying factors>
            if (valueIndex + numberOfTiltAngles + numberOfTiltAngles > totalValueCount)
                throw new Exception("Invalid tilt multiplying factors data count.");

            // parse angles:
            for(int i = 0; i < numberOfTiltAngles; ++i)
            {
                string angleStr = digitValue[valueIndex];
                valueIndex++;

                double angles = 0d;
                if (!double.TryParse(angleStr, out angles))
                    throw new Exception("Parse tileAngle <"+ angleStr + "> failed.");
                tiltInfo.SetAngle(i, angles);
                // TODO: check range.
            }

            // parse multiplying factors:
            for (int i = 0; i < numberOfTiltAngles; ++i)
            {
                string factorStr = digitValue[valueIndex];
                valueIndex++;

                double factor = 0d;
                if (!double.TryParse(factorStr, out factor))
                    throw new Exception("Parse multiplying factors <" + factorStr + "> failed.");
                tiltInfo.SetMultiplyingFactor(i, factor);
            }

            result.SetTiltInfo(IesConfig.TiltInfoType.INCLUDE, tiltInfo);
        }

        /// parse values:
        // based on IESNA:LM-63-2002, there should be at least 13 numbers to create the ies file, witch are:
        /*
         * <number of lamps> <lumens per lamp> <candela multiplier> 
         * <number of vertical angles> <number of horizontal angles>
         * <photometric type> 
         * <units type> 
         * <width> <length> <height> 
         * <ballast factor> <future use> <input watts> 
         * */
        int iesNumValueCount = 13;

        if (valueIndex + iesNumValueCount > totalValueCount)
            throw new Exception("There should be at least 13 numbers.");

        // TODO: value validation check

        // number of lamps:
        string valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!int.TryParse(valueStr, out result.numberOfLamps))
            throw new Exception("Invalid number of lamps value.");

        // lumens per lamp:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!double.TryParse(valueStr, out result.lumenPerLamp))
            throw new Exception("Invalid lumens per lamp value.");

        // candela multiplier:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!double.TryParse(valueStr, out result.candelaMultiplier))
            throw new Exception("Invalid candela multiplier value.");

        // number of vertical angles:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!int.TryParse(valueStr, out result.numberOfVerticalAngles))
            throw new Exception("Invalid number of vertical angles value.");

        // number of horizontal angles:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!int.TryParse(valueStr, out result.numberOfHorizontalAngles))
            throw new Exception("Invalid number of horizontal angles value.");

        // photometric type:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!int.TryParse(valueStr, out result.photometricType))
            throw new Exception("Invalid photometric type value.");

        // units type:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!int.TryParse(valueStr, out result.unitsType))
            throw new Exception("Invalid units type value.");

        // width:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!double.TryParse(valueStr, out result.width))
            throw new Exception("Invalid width value.");

        // length:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!double.TryParse(valueStr, out result.length))
            throw new Exception("Invalid length value.");

        // height:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!double.TryParse(valueStr, out result.height))
            throw new Exception("Invalid height value.");

        // ballast factor:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!double.TryParse(valueStr, out result.ballastFactor))
            throw new Exception("Invalid ballast factor value.");

        // future use:
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!double.TryParse(valueStr, out result.futureUse))
            throw new Exception("Invalid future use value.");

        // input watts
        valueStr = digitValue[valueIndex];
        valueIndex++;
        if (!double.TryParse(valueStr, out result.inputWatts))
            throw new Exception("Invalid input watts value.");

        /// Parse Angle value and intensity value:
        // based on IESNA:LM-63-2002, angle and intensity data are stored as follow:
        /*
         * <vertical angles>        
         * <horizontal angles>
         * <candela values for all vertical angles at 1st horizontal angle>
         * <candela values for all vertical angles as 2nd horizontal angle>
         * ...
         * <candela values for all vertical angles at last horizontal angle>
         * */

        int angleIntensityDataCount = result.numberOfVerticalAngles + result.numberOfHorizontalAngles 
                                    + result.numberOfHorizontalAngles * result.numberOfVerticalAngles;

        if (valueIndex + angleIntensityDataCount > totalValueCount)
            throw new Exception("Angle Intensity data count is invalid.");

        // vertical angles:
        double[] verticalAngles = new double[result.numberOfVerticalAngles];
        for(int i = 0; i < result.numberOfVerticalAngles; ++i)
        {
            valueStr = digitValue[valueIndex];
            valueIndex++;
            double angle = 0d;
            if (!double.TryParse(valueStr, out angle))
                throw new Exception("Invalid vertical angle data.");
            verticalAngles[i] = angle;
        }

        // honrizontal angles:
        double[] horizontalAngles = new double[result.numberOfHorizontalAngles];
        for (int i = 0; i < result.numberOfHorizontalAngles; ++i)
        {
            valueStr = digitValue[valueIndex];
            valueIndex++;
            double angle = 0d;
            if (!double.TryParse(valueStr, out angle))
                throw new Exception("Invalid horizontal angle data.");
            horizontalAngles[i] = angle;
        }

        result.SetVerticalAngles(verticalAngles, false);
        result.SetHorizontalAngles(horizontalAngles, false);

        // raw intensity:
        List<double[]> rawIntensity = new List<double[]>();

        for(int h = 0; h < result.numberOfHorizontalAngles; ++h)
        {
            double[] verticalIntensity = new double[result.numberOfVerticalAngles];
            for (int v = 0; v < result.numberOfVerticalAngles; ++v)
            {
                valueStr = digitValue[valueIndex];
                valueIndex++;

                double intensity;
                if (!double.TryParse(valueStr, out intensity))
                    throw new Exception("Parse intensity data error.");

                verticalIntensity[v] = intensity;
            }
            rawIntensity.Add(verticalIntensity);
        }
        result.SetRawIntensityData(rawIntensity);

        return result;
    }
}

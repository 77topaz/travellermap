﻿using Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Maps
{
    internal delegate bool WorldFilter(World w);

    public class World
    {
        internal Sector Sector { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Hex
        {
            get { return hex.ToString(); }
            set { hex = new Hex(value); }
        }

        internal string SubsectorHex { get { return hex.ToSubsectorString(); } }

        [XmlElement("UWP"), JsonName("UWP")]
        public string UWP { get; set; } = "X000000-0";

        [XmlElement("PBG"), JsonName("PBG")]
        public string PBG { get; set; } = "000";
        public string Zone
        {
            get { return zone; }
            set
            {
                zone = (value == " " || value == "G") ? string.Empty : value;
            }
        }
        private string zone = string.Empty;

        public string Bases { get; set; } = string.Empty;
        public string Allegiance { get; set; } = "Na";
        public string Stellar { get; set; } = string.Empty;

        // T5
        public string SS
        {
            get
            {
                return "" + (char)('A' + Subsector);
            }
        }

        [XmlElement("Ix"), JsonName("Ix")]
        public string Importance { get; set; }

        internal int? ImportanceValue
        {
            get
            {
                int? value = null;
                int tmp;
                string ix = Importance;
                if (!string.IsNullOrWhiteSpace(ix) && int.TryParse(ix.Replace('{', ' ').Replace('}', ' '),
                    NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out tmp))
                {
                    value = tmp;
                }
                return value;
            }
        }

        [XmlElement("Ex"), JsonName("Ex")]
        public string Economic { get; set; }
        [XmlElement("Cx"), JsonName("Cx")]
        public string Cultural { get; set; }
        public string Nobility { get; set; }
        public byte Worlds { get; set; }
        public int ResourceUnits { get; set; }

        private Hex hex;
        internal byte X { get { return hex.X; } }
        internal byte Y { get { return hex.Y; } }

        public int Subsector
        {
            get
            {
                return ((X - 1) / Astrometrics.SubsectorWidth) + ((Y - 1) / Astrometrics.SubsectorHeight) * 4;
            }
        }

        public int Quadrant
        {
            get
            {
                return ((X - 1) / (Astrometrics.SubsectorWidth * 2)) + ((Y - 1) / (Astrometrics.SubsectorHeight * 2)) * 4;
            }
        }


        internal Point Coordinates
        {
            get
            {
                if (Sector == null)
                    throw new InvalidOperationException("Can't get coordinates for a world not assigned to a sector");

                return Astrometrics.LocationToCoordinates(Sector.Location, new Hex(X, Y));
            }
        }

        internal char Starport { get { return UWP[0]; } }
        internal int Size { get { return Char.ToUpperInvariant(UWP[1]) == 'S' ? -1 : SecondSurvey.FromHex(UWP[1], valueIfUnknown: -1); } }
        internal int Atmosphere { get { return SecondSurvey.FromHex(UWP[2], valueIfUnknown: -1); } }
        internal int Hydrographics { get { return SecondSurvey.FromHex(UWP[3], valueIfUnknown: -1); } }
        internal int PopulationExponent { get { return SecondSurvey.FromHex(UWP[4], valueIfUnknown: 0); } }
        internal int Government { get { return SecondSurvey.FromHex(UWP[5], valueIfUnknown: 0); } }
        internal int Law { get { return SecondSurvey.FromHex(UWP[6], valueIfUnknown: 0); } }
        internal int TechLevel { get { return SecondSurvey.FromHex(UWP[8], valueIfUnknown: 0); } }

        internal int PopulationMantissa
        {
            get
            {
                int mantissa = SecondSurvey.FromHex(PBG[0], valueIfUnknown: 0);
                // Hack for legacy data w/o PBG
                if (mantissa == 0 && PopulationExponent > 0)
                    return 1;
                return mantissa;
            }
        }

        internal int Belts { get { return SecondSurvey.FromHex(PBG[1], valueIfUnknown: 0); } }
        internal int GasGiants { get { return SecondSurvey.FromHex(PBG[2], valueIfUnknown: 0); } }
        internal double Population { get { return Math.Pow(10, PopulationExponent) * PopulationMantissa; } }
        internal bool WaterPresent { get { return (Hydrographics > 0) && (Util.InRange(Atmosphere, 2, 9) || Util.InRange(Atmosphere, 0xD, 0xF)); } }
        internal bool IsBa { get { return PopulationExponent == 0; } }
        internal bool IsLo { get { return PopulationExponent < 4; } }
        internal bool IsHi { get { return PopulationExponent >= 9; } }

        internal bool IsAg { get { return Util.InRange(Atmosphere, 4, 9) && Util.InRange(Hydrographics, 4, 8) && Util.InRange(PopulationExponent, 5, 7); } }
        internal bool IsNa { get { return Util.InRange(Atmosphere, 0, 3) && Util.InRange(Hydrographics, 0, 3) && Util.InRange(PopulationExponent, 6, 10); } }
        internal bool IsIn { get { return Util.InList(Atmosphere, 0, 1, 2, 4, 7, 9) && Util.InList(PopulationExponent, 9, 10); } }
        internal bool IsNi { get { return Util.InRange(PopulationExponent, 1, 6); } }
        internal bool IsRi { get { return Util.InList(Atmosphere, 6, 7, 8) && Util.InList(PopulationExponent, 6, 7, 8) && Util.InList(Government, 4, 5, 6, 7, 8, 9); } }
        internal bool IsPo { get { return Util.InList(Atmosphere, 2, 3, 4, 5) && Util.InList(Hydrographics, 0, 1, 2, 3) && PopulationExponent > 0; } }

        internal bool IsWa { get { return Hydrographics == 10; } }
        internal bool IsDe { get { return Util.InRange(Atmosphere, 2, 10) && Hydrographics == 0; } }
        internal bool IsAs { get { return Size == 0; } }
        internal bool IsVa { get { return Util.InRange(Size, 1, 10) && Atmosphere == 0; } }
        internal bool IsIc { get { return Util.InList(Atmosphere, 0, 1) && Util.InRange(Hydrographics, 1, 10); } }
        internal bool IsFl { get { return Atmosphere == 10 && Util.InRange(Hydrographics, 1, 10); } }

        internal bool IsCp { get { return HasCode("Cp"); } }
        internal bool IsCs { get { return HasCode("Cs"); } }
        internal bool IsCx { get { return HasCode("Cx"); } }

        internal bool IsPenalColony { get { return HasCode("Pe"); } }
        internal bool IsReserve { get { return HasCode("Re"); } }
        internal bool IsPrisonExileCamp { get { return HasCode("Px") || HasCode("Ex"); } } // Px is T5, Ex is legacy
        // TODO: "Pr" is used in some legacy files, conflicts with T5 "Pre-Rich" - convert codes on import/export
        internal string ResearchStation { get { return GetCodePrefix("Rs"); } }

        internal bool IsPlaceholder { get { return UWP == "XXXXXXX-X" || UWP == "???????-?"; } }

        internal bool IsCapital
        {
            get
            {
                return codes.Any(s => s == "Cp" || s == "Cs" || s == "Cx" || s == "Capital");
            }
        }

        public bool HasCode(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            return codes.Any(s => s.Equals(code, StringComparison.InvariantCultureIgnoreCase));
        }

        public string GetCodePrefix(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            return codes.Where(s => s.StartsWith(code, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public bool HasCodePrefix(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            return codes.Any(s => s.StartsWith(code, StringComparison.InvariantCultureIgnoreCase));
        }

        // Keep storage as a string and parse on demand to reduce memory consumption.
        // (Many worlds * many codes = lots of strings and container overhead)
        // "[Sophont]" - major race homeworld
        // "(Sophont)" - minor race homeworld
        // "(Sophont)0" - minor race homeworld (population in tenths)
        // "{comment ... }" - arbitrary comment
        // "xyz" - other code
        private class CodeList : IEnumerable<string>
        {
            public CodeList(string codes = "") { this.codes = codes; }
            private string codes;

            public IEnumerator<string> GetEnumerator()
            {
                int pos = 0;                
                while (pos < codes.Length)
                {
                    int begin = pos;
                    switch (codes[pos++])
                    {
                        case ' ':
                            continue;
                        case '[':
                            while (pos < codes.Length && codes[pos] != ']') ++pos;
                            while (pos < codes.Length && codes[pos] != ' ') ++pos;
                            break;
                        case '(':
                            while (pos < codes.Length && codes[pos] != ')') ++pos;
                            while (pos < codes.Length && codes[pos] != ' ') ++pos;
                            break;
                        case '{':
                            while (pos < codes.Length && codes[pos] != '}') ++pos;
                            while (pos < codes.Length && codes[pos] != ' ') ++pos;
                            break;
                        default:
                            while (pos < codes.Length && codes[pos] != ' ') ++pos;
                            break;
                    }
                    yield return codes.Substring(begin, pos - begin);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }

        public string Remarks
        {
            get { return string.Join(" ", codes); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                codes = new CodeList(value);
            }
        }

        internal IEnumerable<string> Codes { get { return codes; } }
        private CodeList codes = new CodeList();

        public string LegacyBaseCode
        {
            get { return SecondSurvey.EncodeLegacyBases(Allegiance, Bases); }
            set { Bases = SecondSurvey.DecodeLegacyBases(Allegiance, value); }
        }

        internal bool IsAmber { get { return Zone == "A" || Zone == "U"; } }
        internal bool IsRed { get { return Zone == "R" || Zone == "F"; } }
        internal bool IsBlue { get { return Zone == "B"; } } // TNE Technologically Elevated Dictatorship

        [XmlAttribute("Sector"), JsonName("Sector")]
        public string SectorName { get { return Sector.Names[0].Text; } }

        public string SubsectorName
        {
            get
            {
                return Sector.Subsector(Subsector)?.Name ?? "";
            }
        }

        public string AllegianceName
        {
            get
            {
                return Sector?.GetAllegianceFromCode(Allegiance)?.Name ?? "";
            }
        }

        internal string BaseAllegiance
        {
            get
            {
                return Sector?.AllegianceCodeToBaseAllegianceCode(Allegiance) ?? Allegiance;
            }
        }

        internal string LegacyAllegiance
        {
            get
            {
                return SecondSurvey.T5AllegianceCodeToLegacyCode(Allegiance);
            }
        }



        internal void Validate(ErrorLogger errors, int lineNumber, string line)
        {
            if (UWP == "???????-?") return;

            Action<string> Error = (string message) => { errors.Error(message, lineNumber, line); };
            Action<bool, string> ErrorIf = (bool test, string message) => { if (test) Error(message); };
            Action<bool, string> ErrorUnless = (bool test, string message) => { if (!test) Error(message); };
            Func<int, string, bool> Check = (int value, string hex) =>
            {
                for (int i = 0; i < hex.Length; ++i)
                {
                    if (value == SecondSurvey.FromHex(hex[i]))
                        return true;
                }
                return false;
            };

            Func<string, bool, bool> CC = (string code, bool calc) =>
            {
                if (calc)
                    ErrorUnless(HasCode(code), $"Missing code: {code}");
                else
                    ErrorUnless(!HasCode(code), $"Extraneous code: {code}");
                return calc;
            };

#if VALIDATE_UWP
            // UWP
            ErrorIf(Atmosphere > 15, $"UWP: Atm>F: {Atmosphere}");
            ErrorIf(Hydrographics > 10, $"UWP: Hyd>A: {Hydrographics}");
            ErrorIf(PopulationExponent > 15, $"UWP: Pop>F: {PopulationExponent}");
            ErrorUnless(Util.InRange(Government, PopulationExponent - 5, Math.Max(15, PopulationExponent + 5)),
                $"UWP: Gov={Government} out of range (Pop={PopulationExponent} + Flux)");
            ErrorUnless(Util.InRange(Law, Government - 5, Math.Max(18, Government + 5)),
                $"UWP: Law={Law} out of range (Gov={Government} + Flux)");
            int tlmod = 
                (Starport == 'A' ? 6 : 0) + (Starport == 'B' ? 4 : 0) + (Starport == 'C' ? 2 : 0) + (Starport == 'X' ? -4 : 0) +
                (Size == 0 || Size == 1 ? 2 : 0) + (Size == 2 || Size == 3 || Size == 4 ? 1 : 0) +
                (Atmosphere <= 3 ? 1 : 0) + (Atmosphere >= 10 ? 1 : 0) +
                (Hydrographics == 9 ? 1 : 0) + (Hydrographics == 10 ? 2 : 0) + 
                (Util.InRange(PopulationExponent, 1, 5) ? 1 : 0) + (PopulationExponent == 9 ? 2 : 0) + (PopulationExponent >= 10 ? 4 : 0) +
                (Government == 0 || Government == 5 ? 1 : 0) + (Government == 13 ? -2 : 0);
            ErrorUnless(Util.InRange(TechLevel, tlmod + 1, tlmod + 6),
                $"UWP: TL={TechLevel} out of range (mods={tlmod} + 1D)");
#endif

            // Planetary
            bool As = CC("As", Check(Size, "0") /*&& Check(Atmosphere, "0") && Check(Hydrographics, "0")*/);
            bool De = CC("De", Check(Atmosphere, "23456789") && Check(Hydrographics, "0"));
            bool Fl = CC("Fl", Check(Atmosphere, "ABC") && Check(Hydrographics, "123456789A"));
            bool Ga = CC("Ga", Check(Size, "678") && Check(Atmosphere, "568") && Check(Hydrographics, "567"));
            bool He = CC("He", Check(Size, "3456789A" /* + "BC" */) && Check(Atmosphere, "2479ABC") && Check(Hydrographics, "012")); // TODO: Add BC to T5SS spreadsheet calcs
            bool Ic = CC("Ic", Check(Atmosphere, "01") && Check(Hydrographics, "123456789A"));
            bool Oc = CC("Oc", Check(Size, "ABC") && Check(Atmosphere, "3456789") && Check(Hydrographics, "A"));
            bool Va = CC("Va", Check(Atmosphere, "0"));
            bool Wa = CC("Wa", Check(Size, "3456789") && Check(Atmosphere, "3456789") && Check(Hydrographics, "A"));

            // Population
            bool Di = CC("Di", PopulationExponent == 0 /*&& Government == 0 && Law == 0*/ && TechLevel > 0);
            bool Ba = CC("Ba", PopulationExponent == 0 /*&& Government == 0 && Law == 0*/ && TechLevel == 0);
            bool Lo = CC("Lo", Check(PopulationExponent, "123"));
            bool Ni = CC("Ni", Check(PopulationExponent, "456"));
            bool Ph = CC("Ph", Check(PopulationExponent, "8"));
            bool Hi = CC("Hi", Check(PopulationExponent, "9ABCDEF"));

            // Economic
            bool Pa = CC("Pa", Check(Atmosphere, "456789") && Check(Hydrographics, "45678") && Check(PopulationExponent, "48"));
            bool Ag = CC("Ag", Check(Atmosphere, "456789") && Check(Hydrographics, "45678") && Check(PopulationExponent, "567"));
            bool Na = CC("Na", Check(Atmosphere, "0123") && Check(Hydrographics, "0123") && Check(PopulationExponent, "6789ABCDEF"));
            bool Pi = CC("Pi", Check(Atmosphere, "012479") && Check(PopulationExponent, "78"));
            bool In = CC("In", Check(Atmosphere, "012479ABC") && Check(PopulationExponent, "9ABCDEF"));
            bool Po = CC("Po", Check(Atmosphere, "2345") && Check(Hydrographics, "0123"));
            bool Pr = CC("Pr", Check(Atmosphere, "68") && Check(PopulationExponent, "59"));
            bool Ri = CC("Ri", Check(Atmosphere, "68") && Check(PopulationExponent, "678"));

            // {Ix}
            int imp = 0;
            {
                string ix = Importance.Replace('{', ' ').Replace('}', ' ').Trim();

                if ("AB".Contains(Starport)) ++imp;
                if ("DEX".Contains(Starport)) --imp;
                if (TechLevel >= 10) ++imp;
                //if (TechLevel >= 16) ++imp; // TODO: Add to T5SS spreadsheet calcs
                if (TechLevel <= 8) --imp;
                if (PopulationExponent <= 6) --imp;
                if (PopulationExponent >= 9) ++imp;
                if (Ag) ++imp;
                if (Ri) ++imp;
                if (In) ++imp;
                if (Bases == "NS" || Bases == "NW" || Bases == "W" || Bases == "X" || Bases == "D" || Bases == "RT" || Bases == "CK" || Bases == "KM") ++imp;

                ErrorUnless(Int32.Parse(ix) == imp,
                    $"{{Ix}}={Importance} does not match calculated Importance={imp}");
            }

            // (Ex)
            {
                string ex = Economic.Replace('(', ' ').Replace(')', ' ').Trim();
                int resources = SecondSurvey.FromHex(ex[0]);
                int labor = SecondSurvey.FromHex(ex[1]);
                int infrastructure = SecondSurvey.FromHex(ex[2]);
                int efficiency = Int32.Parse(ex.Substring(3));

                if (TechLevel < 8)
                    ErrorUnless(Util.InRange(resources, 2, 12),
                        $"(Ex) Resources={resources} out of range for TL<8={TechLevel} (2D)");
                else 
                    ErrorUnless(Util.InRange(resources, 2 + GasGiants + Belts, 12 + GasGiants + Belts),
                        $"(Ex) Resources={resources} out of range for TL8+={TechLevel} (2D + GG={GasGiants} + Belts={Belts})");
                    
                ErrorUnless(labor == Math.Max(0, PopulationExponent - 1),
                    $"(Ex) Labor={labor} does not match Pop={PopulationExponent} - 1");
                    
                if (Ba)
                    ErrorUnless(infrastructure == 0, $"(Ex) Infrastructure={infrastructure} should be 0 if Ba");
                else if (Lo)
                    ErrorUnless(infrastructure == 1, $"(Ex) Infrastructure={infrastructure} should be 1 if Lo");
                else if (Ni)
                    ErrorUnless(Util.InRange(infrastructure, Math.Max(0, imp + 1), Math.Max(0, imp + 6)),
                        $"(Ex) Infrastructure={infrastructure} out of range for Ni (Imp={imp} + 1D)");
                else
                    ErrorUnless(Util.InRange(infrastructure, Math.Max(0, imp + 2), Math.Max(0, imp + 12)),
                        $"(Ex) Infrastructure={infrastructure} out of range (Imp={imp} + 2D)");

                ErrorUnless(Util.InRange(efficiency, -5, 5),
                    $"(Ex) Efficiency={efficiency} out of range (Flux)");
            }

            // [Cx]
            {
                string cx = Cultural.Replace('[', ' ').Replace(']', ' ').Trim();
                int homogeneity = SecondSurvey.FromHex(cx[0]);
                int acceptance = SecondSurvey.FromHex(cx[1]);
                int strangeness = SecondSurvey.FromHex(cx[2]);
                int symbols = SecondSurvey.FromHex(cx[3]);

                if (PopulationExponent == 0)
                {
                    if (homogeneity != 0) Error($"[Cx] Homogeneity={homogeneity} - expected 0 for Pop 0");
                    if (acceptance != 0) Error($"[Cx] Acceptance={acceptance} - expected 0 for Pop 0");
                    if (strangeness != 0) Error($"[Cx] Strangeness={strangeness} - expected 0 for Pop 0");
                    if (symbols != 0) Error($"[Cx] Symbols={symbols} - expected 0 for Pop 0");
                }
                else
                {
                    ErrorUnless(Util.InRange(homogeneity, Math.Max(1, PopulationExponent - 5), Math.Max(1, PopulationExponent + 5)),
                        $"[Cx] Homogeneity={homogeneity} out of range (Pop={PopulationExponent} + Flux)");
                    ErrorUnless(acceptance == Math.Max(1, PopulationExponent + imp),
                        $"[Cx] Acceptance={acceptance} not equal Pop={PopulationExponent} + Imp={imp}");
                    ErrorUnless(Util.InRange(strangeness, Math.Max(1, 5 - 5), Math.Max(1, 5 + 5)),
                        $"[Cx] Strangeness={strangeness} out of range (Flux + 5)");
                    ErrorUnless(Util.InRange(symbols, Math.Max(1, TechLevel - 5), Math.Max(1, TechLevel + 5)),
                        $"[Cx] Symbols={symbols} out of range (TL={TechLevel} + Flux)");
                }
            }

            // Ownership
            if (Government == 6 && !(HasCodePrefix("O:") || HasCodePrefix("Mr") || HasCode("Re") || HasCode("Px")))
                errors.Warning("Gov 6 missing O:/Mr/Re/Px", lineNumber, line);
        }

        private string routes = null;
        [XmlIgnore,JsonIgnore]
        public string Routes
        {
            get
            {
                if (routes == null)
                    routes = string.Join(" ", Sector.RoutesForWorld(this).OrderBy(s => s));
                return routes;
            }
        }
    }
}

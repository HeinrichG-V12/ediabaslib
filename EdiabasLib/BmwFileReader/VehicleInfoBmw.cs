﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EdiabasLib;
#if Android
using ICSharpCode.SharpZipLib.Zip;
#endif

// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace BmwFileReader
{
    public class VehicleInfoBmw
    {
        // ReSharper disable InconsistentNaming
        public enum BusType
        {
            ROOT,
            ETHERNET,
            MOST,
            KCAN,
            KCAN2,
            KCAN3,
            BCAN,
            BCAN2,
            BCAN3,
            FLEXRAY,
            FACAN,
            FASCAN,
            SCAN,
            NONE,
            SIBUS,
            KBUS,
            FCAN,
            ACAN,
            HCAN,
            LOCAN,
            ZGW,
            DWA,
            BYTEFLIGHT,
            INTERNAL,
            VIRTUAL,
            VIRTUALBUSCHECK,
            VIRTUALROOT,
            IBUS,
            LECAN,
            IKCAN,
            UNKNOWN
        }

        public enum BnType
        {
            BN2000,
            BN2020,
            IBUS,
            BN2000_MOTORBIKE,
            BN2020_MOTORBIKE,
            BNK01X_MOTORBIKE,
            BEV2010,
            BN2000_MORGAN,
            BN2000_WIESMANN,
            BN2000_RODING,
            BN2000_PGO,
            BN2000_GIBBS,
            BN2020_CAMPAGNA,
            UNKNOWN
        }
        // ReSharper restore InconsistentNaming

        public interface IEcuLogisticsEntry
        {
            int DiagAddress { get; }

            string Name { get; }

            string GroupSgbd { get; }

            BusType Bus { get; }

            int Column { get; }

            int Row { get; }

            string ShortName { get; }

            long? SubDiagAddress { get; }

            BusType[] SubBusList { get; }
        }

        public class EcuLogisticsEntry : IEcuLogisticsEntry
        {
            public int DiagAddress { get; }
            public string Name { get; }
            public BusType Bus { get; }
            public BusType[] SubBusList { get; }
            public string GroupSgbd { get; }
            public int Column { get; }
            public int Row { get; }
            public string ShortName { get; }
            public long? SubDiagAddress { get; }

            public EcuLogisticsEntry()
            {
            }

            public EcuLogisticsEntry(int diagAddress, string name, BusType bus, string groupSgbd, int column, int row)
                : this(diagAddress, null, name, bus, null, groupSgbd, column, row, null)
            {
            }

            public EcuLogisticsEntry(int diagAddress, string name, BusType bus, BusType[] subBusList, string groupSgbd,
                int column, int row) : this(diagAddress, null, name, bus, subBusList, groupSgbd, column, row, null)
            {
            }

            public EcuLogisticsEntry(int diagAddress, int subDiagAddress, string name, BusType bus, string groupSgbd,
                int column, int row)
                : this(diagAddress, subDiagAddress, name, bus, null, groupSgbd, column, row, null)
            {
            }

            public EcuLogisticsEntry(int diagAddress, string name, BusType bus, string groupSgbd, int column, int row,
                string shortName) : this(diagAddress, null, name, bus, null, groupSgbd, column, row, shortName)
            {
            }

            public EcuLogisticsEntry(int diagAddress, long? subDiagAddress, string name, BusType bus,
                BusType[] subBusList, string groupSgbd, int column, int row, string shortName)
            {
                DiagAddress = diagAddress;
                Name = name;
                Bus = bus;
                SubBusList = subBusList;
                GroupSgbd = groupSgbd;
                Column = column;
                Row = row;
                ShortName = shortName;
                SubDiagAddress = subDiagAddress;
            }
        }

        public class EcuLogisticsData
        {
            public EcuLogisticsData(string xmlName, ReadOnlyCollection<IEcuLogisticsEntry> staticEntry)
            {
                XmlName = xmlName;
                StaticEntry = staticEntry;
                UsageEntry = null;
            }

            public string XmlName { get; }
            public ReadOnlyCollection<IEcuLogisticsEntry> StaticEntry { get; }
            public ReadOnlyCollection<IEcuLogisticsEntry> UsageEntry { get; set; }
        }

        private const string DatabaseFileName = @"Database.zip";

        // ReSharper disable RedundantExplicitArrayCreation
        // ReSharper disable CoVariantArrayConversion

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.E36EcuCharacteristics.E36EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsE36 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_0000|D_ZKE_GM", 1, 1),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 2, 6),
                new EcuLogisticsEntry(13, "KOMBI", BusType.ROOT, "D_000D", 5, 0),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 7, 1),
                new EcuLogisticsEntry(17, "ZKE", BusType.KBUS, "D_0011", 1, 2),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 7, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 7, 1),
                new EcuLogisticsEntry(20, "DME2", BusType.FACAN, "D_0014", 7, 2),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 1, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.UNKNOWN, "D_0016", -1, -1),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 7, 9),
                new EcuLogisticsEntry(25, "ATCU", BusType.UNKNOWN, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.UNKNOWN, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.UNKNOWN, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 7, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.UNKNOWN, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.UNKNOWN, "D_0028", -1, -1),
                new EcuLogisticsEntry(70, "CID", BusType.FACAN, "D_CID", 7, 7),
                new EcuLogisticsEntry(44, "VNC", BusType.UNKNOWN, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.KBUS, "D_0030", 0, 5),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 7, 4),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 7, 4),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 7, 10),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 6, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", 7, 5),
                new EcuLogisticsEntry(59, "VIDEO", BusType.KBUS, "D_003b", 5, 4),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 0, 6),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 2, 1),
                new EcuLogisticsEntry(69, "DWA", BusType.UNKNOWN, "D_0045", -1, -1),
                new EcuLogisticsEntry(41, "DSC/DXC", BusType.KBUS, "D_ABSKWP", 1, 5),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(80, "MFL", BusType.KBUS, "D_0050", 4, 3),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 7, 5),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 7, 6),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.UNKNOWN, "D_005A", -1, -1),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 0, 1),
                new EcuLogisticsEntry(96, "PDC", BusType.KBUS, "D_0060", 2, 3),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 7, 11),
                new EcuLogisticsEntry(102, "ALC", BusType.FACAN, "D_0066", 7, 13),
                new EcuLogisticsEntry(104, "RADIO", BusType.KBUS, "D_0068", 4, 1),
                new EcuLogisticsEntry(105, "EKM", BusType.UNKNOWN, "D_0069", -1, -1),
                new EcuLogisticsEntry(106, "DSP", BusType.KBUS, "D_006A", 1, 4),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 1, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.UNKNOWN, "D_006C", -1, -1),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 0, 3),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.UNKNOWN, "D_0071", -1, -1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 0, 2),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 2, 8),
                new EcuLogisticsEntry(118, "CDC", BusType.KBUS, "D_0076", 5, 1),
                new EcuLogisticsEntry(127, "NAVI", BusType.KBUS, "D_007f", 5, 2),
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.KBUS, "D_0081", 4, 4),
                new EcuLogisticsEntry(134, "AHK", BusType.UNKNOWN, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 2, 2),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", 2, 4),
                new EcuLogisticsEntry(157, "ETS", BusType.UNKNOWN, "D_009d", -1, -1),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 2, 4),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 2, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.KBUS, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.SIBUS, "D_SBSL2", -1, -1),
                new EcuLogisticsEntry(162, "SBSR", BusType.SIBUS, "D_SBSR2", -1, -1),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, "D_00a4|D_SIM", 2, 5),
                new EcuLogisticsEntry(166, "GR", BusType.FACAN, "D_00a6", 6, 5),
                new EcuLogisticsEntry(167, "FHK", BusType.KBUS, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 7, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.SIBUS, "D_STVL", -1, -1),
                new EcuLogisticsEntry(174, "STVR", BusType.SIBUS, "D_STVR", -1, -1),
                new EcuLogisticsEntry(176, "RADIO", BusType.KBUS, "D_00b0", -1, -1),
                new EcuLogisticsEntry(185, "IRS", BusType.KBUS, "D_00b9", 0, 8),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 0, 7),
                new EcuLogisticsEntry(194, "SVT", BusType.KBUS, "D_00c2", 1, 6),
                new EcuLogisticsEntry(200, "TELEFON", BusType.KBUS, "D_00c8", 4, 5),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 2, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.SIBUS, "D_00ce", -1, -1),
                new EcuLogisticsEntry(208, "LM", BusType.FACAN, "D_00d0", 7, 8),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 0, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.UNKNOWN, "D_00e0", -1, -1),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 1, 3),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.KBUS, "D_00ea", -1, -1),
                new EcuLogisticsEntry(237, "VIDEO", BusType.KBUS, "D_00ed", 4, 7),
                new EcuLogisticsEntry(240, "CIDB", BusType.KBUS, "D_00f0", 4, 6),
                new EcuLogisticsEntry(245, "LKM", BusType.KBUS, "D_00f5|d_szm", 4, 2)
            });
        public static EcuLogisticsData EcuLogisticsDataE36 =
            new EcuLogisticsData("E36EcuCharacteristics.xml", EcuLogisticsE36);

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.E38EcuCharacteristics.E38EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsE38 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_ZKE_GM", 1, 1),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 2, 6),
                new EcuLogisticsEntry(13, "KOMBI", BusType.FACAN, "D_000D", -1, -1),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 7, 1),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 7, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 7, 1),
                new EcuLogisticsEntry(20, "DME2", BusType.FACAN, "D_0014", 7, 2),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 1, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.UNKNOWN, "D_0016", -1, -1),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 7, 9),
                new EcuLogisticsEntry(25, "ATCU", BusType.UNKNOWN, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.UNKNOWN, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.UNKNOWN, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 7, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.UNKNOWN, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.IBUS, "D_0028", 4, 8),
                new EcuLogisticsEntry(70, "CID", BusType.FACAN, "D_CID", 7, 7),
                new EcuLogisticsEntry(44, "VNC", BusType.UNKNOWN, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.IBUS, "D_0030", 5, 5),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 7, 4),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 7, 4),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 7, 10),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 6, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", -1, -1),
                new EcuLogisticsEntry(59, "VIDEO", BusType.IBUS, "D_003b", 5, 4),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 0, 6),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 2, 1),
                new EcuLogisticsEntry(69, "DWA", BusType.UNKNOWN, "D_0045", -1, -1),
                new EcuLogisticsEntry(41, "DSC/DXC", BusType.KBUS, "D_ABSKWP", 1, 5),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(80, "MFL", BusType.IBUS, "D_0050", 4, 3),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 7, 5),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 7, 6),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.UNKNOWN, "D_005A", -1, -1),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 0, 1),
                new EcuLogisticsEntry(96, "PDC", BusType.IBUS, "D_0060", 5, 3),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 7, 11),
                new EcuLogisticsEntry(102, "ALC", BusType.FACAN, "D_0066", 7, 13),
                new EcuLogisticsEntry(104, "RADIO", BusType.IBUS, "D_0068", 4, 1),
                new EcuLogisticsEntry(105, "EKM", BusType.UNKNOWN, "D_0069", -1, -1),
                new EcuLogisticsEntry(106, "DSP", BusType.IBUS, "D_006A", 1, 4),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 1, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.UNKNOWN, "D_006C", -1, -1),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 0, 3),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.UNKNOWN, "D_0071", -1, -1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 0, 2),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 2, 8),
                new EcuLogisticsEntry(118, "CDC", BusType.IBUS, "D_0076", 5, 1),
                new EcuLogisticsEntry(127, "NAVI", BusType.IBUS, "D_007f", 5, 2),
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.IBUS, "D_0081", 4, 4),
                new EcuLogisticsEntry(134, "AHK", BusType.UNKNOWN, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 2, 2),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", -1, -1),
                new EcuLogisticsEntry(157, "ETS", BusType.UNKNOWN, "D_009d", -1, -1),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 2, 4),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 2, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.KBUS, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.SIBUS, "D_SBSL2", -1, -1),
                new EcuLogisticsEntry(162, "SBSR", BusType.SIBUS, "D_SBSR2", -1, -1),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, "D_00a4|D_SIM", 2, 5),
                new EcuLogisticsEntry(166, "GR", BusType.FACAN, "D_00a6", 6, 5),
                new EcuLogisticsEntry(167, "FHK", BusType.KBUS, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 7, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.SIBUS, "D_STVL", -1, -1),
                new EcuLogisticsEntry(174, "STVR", BusType.SIBUS, "D_STVR", -1, -1),
                new EcuLogisticsEntry(176, "RADIO", BusType.KBUS, "D_00b0", -1, -1),
                new EcuLogisticsEntry(184, "ACC", BusType.FACAN, "D_b8_d0", 6, 6),
                new EcuLogisticsEntry(185, "IRS", BusType.KBUS, "D_00b9", -1, -1),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 0, 7),
                new EcuLogisticsEntry(194, "SVT", BusType.KBUS, "D_00c2", 1, 6),
                new EcuLogisticsEntry(200, "TELEFON", BusType.IBUS, "D_00c8", 4, 5),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 2, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.SIBUS, "D_00ce", -1, -1),
                new EcuLogisticsEntry(208, "LM", BusType.IBUS, "D_00d0", 5, 6),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 0, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.UNKNOWN, "D_00e0", -1, -1),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 1, 3),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.KBUS, "D_00ea", -1, -1),
                new EcuLogisticsEntry(237, "VIDEO", BusType.IBUS, "D_00ed", 4, 7),
                new EcuLogisticsEntry(240, "CIDB", BusType.IBUS, "D_00f0", 4, 6),
                new EcuLogisticsEntry(245, "LKM", BusType.IBUS, "D_00f5|d_szm", 4, 2)
            });
        public static EcuLogisticsData EcuLogisticsDataE38 =
            new EcuLogisticsData("E38EcuCharacteristics.xml", EcuLogisticsE38);

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.E39EcuCharacteristics.E39EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsE39 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_0000|D_ZKE_GM", 1, 1),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 2, 6),
                new EcuLogisticsEntry(13, "KOMBI", BusType.FACAN, "D_000D", -1, -1),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 7, 1),
                new EcuLogisticsEntry(17, "ZKE", BusType.KBUS, "D_0011", 1, 2),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 7, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 7, 1),
                new EcuLogisticsEntry(20, "DME2", BusType.FACAN, "D_0014", 7, 2),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 1, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.UNKNOWN, "D_0016", -1, -1),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 7, 9),
                new EcuLogisticsEntry(25, "ATCU", BusType.UNKNOWN, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.UNKNOWN, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.UNKNOWN, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 7, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.UNKNOWN, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.UNKNOWN, "D_0028", -1, -1),
                new EcuLogisticsEntry(70, "CID", BusType.FACAN, "D_CID", 7, 7),
                new EcuLogisticsEntry(44, "VNC", BusType.UNKNOWN, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.KBUS, "D_0030", 0, 5),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 7, 4),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 7, 4),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 7, 10),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 6, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", -1, -1),
                new EcuLogisticsEntry(59, "VIDEO", BusType.KBUS, "D_003b", 5, 4),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 0, 6),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 2, 1),
                new EcuLogisticsEntry(69, "DWA", BusType.UNKNOWN, "D_0045", -1, -1),
                new EcuLogisticsEntry(41, "DSC/DXC", BusType.KBUS, "D_ABSKWP", 1, 5),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(80, "MFL", BusType.KBUS, "D_0050", 4, 3),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 7, 5),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 7, 6),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.UNKNOWN, "D_005A", -1, -1),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 0, 1),
                new EcuLogisticsEntry(96, "PDC", BusType.KBUS, "D_0060", 2, 3),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 7, 11),
                new EcuLogisticsEntry(102, "ALC", BusType.FACAN, "D_0066", 7, 13),
                new EcuLogisticsEntry(104, "RADIO", BusType.KBUS, "D_0068", 4, 1),
                new EcuLogisticsEntry(105, "EKM", BusType.UNKNOWN, "D_0069", -1, -1),
                new EcuLogisticsEntry(106, "DSP", BusType.KBUS, "D_006A", 1, 4),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 1, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.UNKNOWN, "D_006C", -1, -1),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 0, 3),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.UNKNOWN, "D_0071", -1, -1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 0, 2),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 2, 8),
                new EcuLogisticsEntry(118, "CDC", BusType.KBUS, "D_0076", 5, 1),
                new EcuLogisticsEntry(127, "NAVI", BusType.KBUS, "D_007f", 5, 2),
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.KBUS, "D_0081", 4, 4),
                new EcuLogisticsEntry(134, "AHK", BusType.UNKNOWN, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 2, 2),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", -1, -1),
                new EcuLogisticsEntry(157, "ETS", BusType.UNKNOWN, "D_009d", -1, -1),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 2, 4),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 2, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.KBUS, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.SIBUS, "D_SBSL2", -1, -1),
                new EcuLogisticsEntry(162, "SBSR", BusType.SIBUS, "D_SBSR2", -1, -1),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, "D_00a4|D_SIM", 2, 5),
                new EcuLogisticsEntry(166, "GR", BusType.FACAN, "D_00a6", 6, 5),
                new EcuLogisticsEntry(167, "FHK", BusType.KBUS, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 7, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.SIBUS, "D_STVL", -1, -1),
                new EcuLogisticsEntry(174, "STVR", BusType.SIBUS, "D_STVR", -1, -1),
                new EcuLogisticsEntry(176, "RADIO", BusType.KBUS, "D_00b0", -1, -1),
                new EcuLogisticsEntry(185, "IRS", BusType.KBUS, "D_00b9", -1, -1),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 0, 7),
                new EcuLogisticsEntry(194, "SVT", BusType.KBUS, "D_00c2", 1, 6),
                new EcuLogisticsEntry(200, "TELEFON", BusType.KBUS, "D_00c8", 4, 5),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 2, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.SIBUS, "D_00ce", -1, -1),
                new EcuLogisticsEntry(208, "LM", BusType.FACAN, "D_00d0", 7, 8),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 0, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.UNKNOWN, "D_00e0", -1, -1),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 1, 3),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.KBUS, "D_00ea", -1, -1),
                new EcuLogisticsEntry(237, "VIDEO", BusType.KBUS, "D_00ed", 4, 7),
                new EcuLogisticsEntry(240, "CIDB", BusType.KBUS, "D_00f0", 4, 6),
                new EcuLogisticsEntry(245, "LKM", BusType.KBUS, "D_00f5|d_szm", 4, 2)
            });
        public static EcuLogisticsData EcuLogisticsDataE39 =
            new EcuLogisticsData("E39EcuCharacteristics.xml", EcuLogisticsE39);

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.E46EcuCharacteristics.E46EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsE46 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_0000|D_ZKE_GM", 1, 1),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 2, 6),
                new EcuLogisticsEntry(13, "KOMBI", BusType.FACAN, "D_000D", -1, -1),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 7, 1),
                new EcuLogisticsEntry(17, "ZKE", BusType.KBUS, "D_0011", 1, 2),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 7, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 7, 1),
                new EcuLogisticsEntry(20, "DME2", BusType.FACAN, "D_0014", 7, 2),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 1, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.UNKNOWN, "D_0016", -1, -1),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 7, 9),
                new EcuLogisticsEntry(25, "ATCU", BusType.UNKNOWN, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.UNKNOWN, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.UNKNOWN, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 7, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.UNKNOWN, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.UNKNOWN, "D_0028", -1, -1),
                new EcuLogisticsEntry(70, "CID", BusType.FACAN, "D_CID", 7, 7),
                new EcuLogisticsEntry(44, "VNC", BusType.UNKNOWN, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.KBUS, "D_0030", 0, 5),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 7, 4),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 7, 4),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 7, 10),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 6, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", 7, 5),
                new EcuLogisticsEntry(59, "VIDEO", BusType.KBUS, "D_003b", 5, 4),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 0, 6),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 2, 1),
                new EcuLogisticsEntry(69, "DWA", BusType.UNKNOWN, "D_0045", -1, -1),
                new EcuLogisticsEntry(41, "DSC/DXC", BusType.FACAN, "D_ABSKWP", 7, 5),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(80, "MFL", BusType.KBUS, "D_0050", 4, 3),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 7, 5),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 7, 6),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.UNKNOWN, "D_005A", -1, -1),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 0, 1),
                new EcuLogisticsEntry(96, "PDC", BusType.KBUS, "D_0060", 2, 3),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 7, 11),
                new EcuLogisticsEntry(102, "ALC", BusType.FACAN, "D_0066", 7, 13),
                new EcuLogisticsEntry(104, "RADIO", BusType.KBUS, "D_0068", 4, 1),
                new EcuLogisticsEntry(105, "EKM", BusType.UNKNOWN, "D_0069", -1, -1),
                new EcuLogisticsEntry(106, "DSP", BusType.KBUS, "D_006A", 1, 4),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 1, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.UNKNOWN, "D_006C", -1, -1),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 0, 3),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.UNKNOWN, "D_0071", -1, -1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 0, 2),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 2, 8),
                new EcuLogisticsEntry(118, "CDC", BusType.KBUS, "D_0076", 5, 1),
                new EcuLogisticsEntry(127, "NAVI", BusType.KBUS, "D_007f", 5, 2),
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.KBUS, "D_0081", 4, 4),
                new EcuLogisticsEntry(134, "AHK", BusType.UNKNOWN, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 2, 2),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", -1, -1),
                new EcuLogisticsEntry(157, "ETS", BusType.UNKNOWN, "D_009d", -1, -1),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 2, 4),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 2, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.UNKNOWN, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.SIBUS, "D_SBSL2", -1, -1),
                new EcuLogisticsEntry(162, "SBSR", BusType.SIBUS, "D_SBSR2", -1, -1),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, "D_00a4|D_SIM", 2, 5),
                new EcuLogisticsEntry(166, "GR", BusType.FACAN, "D_00a6", 6, 5),
                new EcuLogisticsEntry(167, "FHK", BusType.UNKNOWN, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 7, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.SIBUS, "D_STVL", -1, -1),
                new EcuLogisticsEntry(174, "STVR", BusType.SIBUS, "D_STVR", -1, -1),
                new EcuLogisticsEntry(176, "RADIO", BusType.KBUS, "D_00b0", -1, -1),
                new EcuLogisticsEntry(185, "IRS", BusType.UNKNOWN, "D_00b9", -1, -1),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 0, 7),
                new EcuLogisticsEntry(194, "SVT", BusType.KBUS, "D_00c2", 1, 6),
                new EcuLogisticsEntry(200, "TELEFON", BusType.KBUS, "D_00c8", 4, 5),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 2, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.SIBUS, "D_00ce", -1, -1),
                new EcuLogisticsEntry(208, "LM", BusType.FACAN, "D_00d0", 7, 8),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 0, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.UNKNOWN, "D_00e0", -1, -1),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 1, 3),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.UNKNOWN, "D_00ea", -1, -1),
                new EcuLogisticsEntry(237, "VIDEO", BusType.KBUS, "D_00ed", 4, 7),
                new EcuLogisticsEntry(240, "CIDB", BusType.KBUS, "D_00f0", 4, 6),
                new EcuLogisticsEntry(245, "LKM", BusType.KBUS, "D_00f5|d_szm", 4, 2)
            });
        public static EcuLogisticsData EcuLogisticsDataE46 =
            new EcuLogisticsData("E46EcuCharacteristics.xml", EcuLogisticsE46);

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.E52EcuCharacteristics.E52EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsE52 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_0000|D_ZKE_GM", 1, 1),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 2, 6),
                new EcuLogisticsEntry(13, "KOMBI", BusType.FACAN, "D_000D", -1, -1),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 7, 1),
                new EcuLogisticsEntry(17, "ZKE", BusType.KBUS, "D_0011", 1, 2),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 7, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 7, 1),
                new EcuLogisticsEntry(20, "DME2", BusType.FACAN, "D_0014", 7, 2),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 1, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.UNKNOWN, "D_0016", -1, -1),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 7, 9),
                new EcuLogisticsEntry(25, "ATCU", BusType.UNKNOWN, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.UNKNOWN, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.UNKNOWN, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 7, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.UNKNOWN, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.UNKNOWN, "D_0028", -1, -1),
                new EcuLogisticsEntry(70, "CID", BusType.FACAN, "D_CID", 7, 7),
                new EcuLogisticsEntry(44, "VNC", BusType.UNKNOWN, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.KBUS, "D_0030", 0, 5),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 7, 4),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 7, 4),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 7, 10),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 6, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", -1, -1),
                new EcuLogisticsEntry(59, "VIDEO", BusType.KBUS, "D_003b", 5, 4),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 0, 6),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 2, 1),
                new EcuLogisticsEntry(69, "DWA", BusType.UNKNOWN, "D_0045", -1, -1),
                new EcuLogisticsEntry(41, "DSC/DXC", BusType.KBUS, "D_ABSKWP", 1, 5),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(80, "MFL", BusType.KBUS, "D_0050", 4, 3),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 7, 5),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 7, 6),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.UNKNOWN, "D_005A", -1, -1),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 0, 1),
                new EcuLogisticsEntry(96, "PDC", BusType.KBUS, "D_0060", 2, 3),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 7, 11),
                new EcuLogisticsEntry(102, "ALC", BusType.FACAN, "D_0066", 7, 13),
                new EcuLogisticsEntry(104, "RADIO", BusType.KBUS, "D_0068", 4, 1),
                new EcuLogisticsEntry(105, "EKM", BusType.UNKNOWN, "D_0069", -1, -1),
                new EcuLogisticsEntry(106, "DSP", BusType.KBUS, "D_006A", 1, 4),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 1, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.UNKNOWN, "D_006C", -1, -1),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 0, 3),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.UNKNOWN, "D_0071", -1, -1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 0, 2),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 2, 8),
                new EcuLogisticsEntry(118, "CDC", BusType.KBUS, "D_0076", 5, 1),
                new EcuLogisticsEntry(127, "NAVI", BusType.KBUS, "D_007f", 5, 2),
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.KBUS, "D_0081", 4, 4),
                new EcuLogisticsEntry(134, "AHK", BusType.UNKNOWN, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 2, 2),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", -1, -1),
                new EcuLogisticsEntry(157, "ETS", BusType.UNKNOWN, "D_009d", -1, -1),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 2, 4),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 2, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.UNKNOWN, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.SIBUS, "D_SBSL2", -1, -1),
                new EcuLogisticsEntry(162, "SBSR", BusType.SIBUS, "D_SBSR2", -1, -1),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, "D_00a4|D_SIM", 2, 5),
                new EcuLogisticsEntry(166, "GR", BusType.FACAN, "D_00a6", 6, 5),
                new EcuLogisticsEntry(167, "FHK", BusType.UNKNOWN, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 7, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.SIBUS, "D_STVL", -1, -1),
                new EcuLogisticsEntry(174, "STVR", BusType.SIBUS, "D_STVR", -1, -1),
                new EcuLogisticsEntry(176, "RADIO", BusType.KBUS, "D_00b0", -1, -1),
                new EcuLogisticsEntry(185, "IRS", BusType.UNKNOWN, "D_00b9", -1, -1),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 0, 7),
                new EcuLogisticsEntry(194, "SVT", BusType.KBUS, "D_00c2", 1, 6),
                new EcuLogisticsEntry(200, "TELEFON", BusType.KBUS, "D_00c8", 4, 5),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 2, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.SIBUS, "D_00ce", -1, -1),
                new EcuLogisticsEntry(208, "LM", BusType.FACAN, "D_00d0", 7, 8),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 0, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.UNKNOWN, "D_00e0", -1, -1),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 1, 3),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.UNKNOWN, "D_00ea", -1, -1),
                new EcuLogisticsEntry(237, "VIDEO", BusType.KBUS, "D_00ed", 4, 7),
                new EcuLogisticsEntry(240, "CIDB", BusType.KBUS, "D_00f0", 4, 6),
                new EcuLogisticsEntry(245, "LKM", BusType.KBUS, "D_00f5|d_szm", 4, 2)
            });
        public static EcuLogisticsData EcuLogisticsDataE52 =
            new EcuLogisticsData("E52EcuCharacteristics.xml", EcuLogisticsE52);

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.E53EcuCharacteristics.E53EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsE53 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_ZKE_GM|D_0000", 1, 1),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 2, 6),
                new EcuLogisticsEntry(13, "KOMBI", BusType.FACAN, "D_000D", -1, -1),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 7, 1),
                new EcuLogisticsEntry(17, "ZKE", BusType.KBUS, "D_0011", 1, 2),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 7, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 7, 1),
                new EcuLogisticsEntry(20, "DME2", BusType.FACAN, "D_0014", 7, 2),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 1, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.UNKNOWN, "D_0016", -1, -1),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 7, 9),
                new EcuLogisticsEntry(25, "ATCU", BusType.UNKNOWN, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.UNKNOWN, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.UNKNOWN, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 7, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.UNKNOWN, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.UNKNOWN, "D_0028", -1, -1),
                new EcuLogisticsEntry(41, "DXC", BusType.FACAN, "D_ABSKWP", 6, 5),
                new EcuLogisticsEntry(70, "CID", BusType.FACAN, "D_CID", 7, 7),
                new EcuLogisticsEntry(44, "VNC", BusType.UNKNOWN, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.KBUS, "D_0030", 0, 5),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 7, 4),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 7, 4),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 7, 10),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 6, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", -1, -1),
                new EcuLogisticsEntry(59, "VIDEO", BusType.KBUS, "D_003b", 5, 4),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 0, 6),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 2, 1),
                new EcuLogisticsEntry(69, "DWA", BusType.UNKNOWN, "D_0045", -1, -1),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(80, "MFL", BusType.KBUS, "D_0050", 4, 3),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 7, 5),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 7, 6),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.UNKNOWN, "D_005A", -1, -1),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 0, 1),
                new EcuLogisticsEntry(96, "PDC", BusType.KBUS, "D_0060", 2, 3),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 7, 11),
                new EcuLogisticsEntry(102, "ALC", BusType.FACAN, "D_0066", 7, 13),
                new EcuLogisticsEntry(104, "RADIO", BusType.KBUS, "D_0068", 4, 1),
                new EcuLogisticsEntry(105, "EKM", BusType.UNKNOWN, "D_0069", -1, -1),
                new EcuLogisticsEntry(106, "DSP", BusType.KBUS, "D_006A", 1, 4),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 1, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.UNKNOWN, "D_006C", -1, -1),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 0, 3),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.UNKNOWN, "D_0071", -1, -1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 0, 2),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 2, 8),
                new EcuLogisticsEntry(118, "CDC", BusType.KBUS, "D_0076", 5, 1),
                new EcuLogisticsEntry(127, "NAVI", BusType.KBUS, "D_007f", 5, 2),
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.KBUS, "D_0081", 4, 4),
                new EcuLogisticsEntry(134, "AHK", BusType.UNKNOWN, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 2, 2),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", -1, -1),
                new EcuLogisticsEntry(157, "ETS", BusType.UNKNOWN, "D_009d", -1, -1),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 2, 4),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 2, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.UNKNOWN, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.SIBUS, "D_SBSL2", -1, -1),
                new EcuLogisticsEntry(162, "SBSR", BusType.SIBUS, "D_SBSR2", -1, -1),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, "D_00a4|D_SIM", 2, 5),
                new EcuLogisticsEntry(166, "GR", BusType.FACAN, "D_00a6", 6, 5),
                new EcuLogisticsEntry(167, "FHK", BusType.UNKNOWN, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 7, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.SIBUS, "D_STVL", -1, -1),
                new EcuLogisticsEntry(174, "STVR", BusType.SIBUS, "D_STVR", -1, -1),
                new EcuLogisticsEntry(176, "RADIO", BusType.KBUS, "D_00b0", -1, -1),
                new EcuLogisticsEntry(185, "IRS", BusType.UNKNOWN, "D_00b9", -1, -1),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 0, 7),
                new EcuLogisticsEntry(194, "SVT", BusType.KBUS, "D_00c2", 1, 6),
                new EcuLogisticsEntry(200, "TELEFON", BusType.KBUS, "D_00c8", 4, 5),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 2, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.SIBUS, "D_00ce", -1, -1),
                new EcuLogisticsEntry(208, "LM", BusType.KBUS, "D_00d0", 0, 8),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 0, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.UNKNOWN, "D_00e0", -1, -1),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 1, 3),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.UNKNOWN, "D_00ea", -1, -1),
                new EcuLogisticsEntry(237, "VIDEO", BusType.KBUS, "D_00ed", 4, 7),
                new EcuLogisticsEntry(240, "CIDB", BusType.KBUS, "D_00f0", 4, 6),
                new EcuLogisticsEntry(245, "LKM", BusType.KBUS, "D_00f5|d_szm", 4, 2)
            });
        public static EcuLogisticsData EcuLogisticsDataE53 =
            new EcuLogisticsData("E53EcuCharacteristics.xml", EcuLogisticsE53);

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.E83EcuCharacteristics.E83EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsE83 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.INTERNAL, "D_0081", 5, 1),
                new EcuLogisticsEntry(70, "CID", BusType.KBUS, "D_CID", 1, 1),
                new EcuLogisticsEntry(106, "DSP", BusType.KBUS, "D_006A", 1, 2),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 1, 3),
                new EcuLogisticsEntry(59, "VIDEO", BusType.KBUS, "D_003b", 2, 1),
                new EcuLogisticsEntry(200, "TELEFON", BusType.KBUS, "D_00c8", 2, 2),
                new EcuLogisticsEntry(245, "LKM", BusType.KBUS, "D_00f5|d_SZM", 2, 3),
                new EcuLogisticsEntry(127, "NAVI", BusType.KBUS, "D_007f", 2, 4),
                new EcuLogisticsEntry(118, "CDC", BusType.KBUS, "D_0076", 2, 5),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 3, 1),
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_0000|D_ZKE_GM", 3, 2),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 3, 3),
                new EcuLogisticsEntry(208, "LM", BusType.KBUS, "D_00d0", 3, 4),
                new EcuLogisticsEntry(80, "MFL", BusType.KBUS, "D_0050", 3, 5),
                new EcuLogisticsEntry(176, "SES", BusType.KBUS, "D_00b0", 3, 6),
                new EcuLogisticsEntry(96, "PDC", BusType.KBUS, "D_0060", 3, 7),
                new EcuLogisticsEntry(104, "RADIO", BusType.KBUS, "D_0068", 3, 8),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 4, 1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 4, 3),
                new EcuLogisticsEntry(102, "ALC", BusType.KBUS, "D_0066", 4, 4),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 4, 5),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, "D_00a4|D_SIM", 4, 6),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 4, 8),
                new EcuLogisticsEntry(41, "ABS/DSC", BusType.FACAN, "D_ABSKWP", 7, 1),
                new EcuLogisticsEntry(194, "SVT", BusType.FACAN, "D_00c2", 7, 2),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 7, 3),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 7, 7),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 8, 1),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 8, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 8, 1),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 8, 3),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 8, 5),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 7, 2),
                new EcuLogisticsEntry(13, "KOMBI", BusType.FACAN, "D_000D", -1, -1),
                new EcuLogisticsEntry(17, "ZKE", BusType.KBUS, "D_0011", 2, 2),
                new EcuLogisticsEntry(237, "VIDEO", BusType.KBUS, "D_00ed", 2, 1),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 2, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.UNKNOWN, "D_0016", -1, -1),
                new EcuLogisticsEntry(25, "ATCU", BusType.UNKNOWN, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.UNKNOWN, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.UNKNOWN, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 7, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.UNKNOWN, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.UNKNOWN, "D_0028", -1, -1),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", 2, 10),
                new EcuLogisticsEntry(44, "VNC", BusType.UNKNOWN, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.KBUS, "D_0030", 1, 5),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 7, 4),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 2, 1),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 6, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", -1, -1),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 1, 6),
                new EcuLogisticsEntry(69, "DWA", BusType.UNKNOWN, "D_0045", -1, -1),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 7, 5),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.UNKNOWN, "D_005A", -1, -1),
                new EcuLogisticsEntry(240, "CIDB", BusType.KBUS, "D_00f0", 2, 7),
                new EcuLogisticsEntry(105, "EKM", BusType.UNKNOWN, "D_0069", -1, -1),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 2, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.UNKNOWN, "D_006C", -1, -1),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.UNKNOWN, "D_0071", -1, -1),
                new EcuLogisticsEntry(134, "AHK", BusType.UNKNOWN, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", -1, -1),
                new EcuLogisticsEntry(157, "ETS", BusType.UNKNOWN, "D_009d", -1, -1),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 3, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.UNKNOWN, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.SIBUS, "D_SBSL", -1, -1),
                new EcuLogisticsEntry(162, "SBSR", BusType.SIBUS, "D_SBSR", -1, -1),
                new EcuLogisticsEntry(166, "GR", BusType.UNKNOWN, "D_00a6", -1, -1),
                new EcuLogisticsEntry(167, "FHK", BusType.UNKNOWN, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 7, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.SIBUS, "D_STVL", -1, -1),
                new EcuLogisticsEntry(174, "STVR", BusType.SIBUS, "D_STVR", -1, -1),
                new EcuLogisticsEntry(185, "IRS", BusType.UNKNOWN, "D_00b9", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 1, 7),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 3, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.SIBUS, "D_00ce", -1, -1),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 1, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.UNKNOWN, "D_00e0", -1, -1),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 2, 3),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.UNKNOWN, "D_00ea", -1, -1)
            });
        public static EcuLogisticsData EcuLogisticsDataE83 =
            new EcuLogisticsData("E83EcuCharacteristics.xml", EcuLogisticsE83);

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.E85EcuCharacteristics.E85EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsE85 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_0000|D_ZKE_GM", 1, 1),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 2, 6),
                new EcuLogisticsEntry(13, "KOMBI", BusType.FACAN, "D_000D", -1, -1),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 9, 1),
                new EcuLogisticsEntry(17, "ZKE", BusType.KBUS, "D_0011", 1, 2),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 9, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 9, 1),
                new EcuLogisticsEntry(20, "DME2", BusType.FACAN, "D_0014", 9, 2),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 1, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.UNKNOWN, "D_0016", -1, -1),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 9, 9),
                new EcuLogisticsEntry(25, "ATCU", BusType.UNKNOWN, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.UNKNOWN, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.UNKNOWN, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 9, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.UNKNOWN, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.UNKNOWN, "D_0028", -1, -1),
                new EcuLogisticsEntry(70, "CID", BusType.FACAN, "D_CID", 9, 7),
                new EcuLogisticsEntry(44, "VNC", BusType.UNKNOWN, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.KBUS, "D_0030", 0, 5),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 9, 4),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 9, 4),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 9, 10),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 8, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", -1, -1),
                new EcuLogisticsEntry(55, "EPS", BusType.FACAN, "D_EPS", -1, -1),
                new EcuLogisticsEntry(59, "VIDEO", BusType.KBUS, "D_003b", 5, 4),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 0, 6),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 2, 1),
                new EcuLogisticsEntry(69, "DWA", BusType.UNKNOWN, "D_0045", -1, -1),
                new EcuLogisticsEntry(41, "DSC/DXC", BusType.FACAN, "D_ABSKWP", 8, 2),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(80, "MFL", BusType.KBUS, "D_0050", 4, 3),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 9, 5),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 9, 6),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.UNKNOWN, "D_005A", -1, -1),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 0, 1),
                new EcuLogisticsEntry(96, "PDC", BusType.KBUS, "D_0060", 2, 3),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 9, 11),
                new EcuLogisticsEntry(102, "ALC", BusType.FACAN, "D_0066", 9, 13),
                new EcuLogisticsEntry(104, "RADIO", BusType.KBUS, "D_0068", 4, 1),
                new EcuLogisticsEntry(105, "EKM", BusType.UNKNOWN, "D_0069", -1, -1),
                new EcuLogisticsEntry(106, "DSP", BusType.KBUS, "D_006A", 1, 4),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 1, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.UNKNOWN, "D_006C", -1, -1),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 0, 3),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.UNKNOWN, "D_0071", -1, -1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 0, 2),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 2, 8),
                new EcuLogisticsEntry(118, "CDC", BusType.KBUS, "D_0076", 5, 1),
                new EcuLogisticsEntry(127, "NAVI", BusType.KBUS, "D_007f", 5, 2),
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.KBUS, "D_0081", 4, 4),
                new EcuLogisticsEntry(134, "AHK", BusType.UNKNOWN, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 2, 2),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", -1, -1),
                new EcuLogisticsEntry(157, "ETS", BusType.UNKNOWN, "D_009d", -1, -1),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 2, 4),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 2, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.UNKNOWN, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.BYTEFLIGHT, "D_SBSL2", 3, 5),
                new EcuLogisticsEntry(162, "SBSR", BusType.BYTEFLIGHT, "D_SBSR2", 3, 6),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, new BusType[] {BusType.BYTEFLIGHT}, "D_00a4|D_SIM", 2, 5),
                new EcuLogisticsEntry(166, "GR", BusType.FACAN, "D_00a6", 8, 5),
                new EcuLogisticsEntry(167, "FHK", BusType.UNKNOWN, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 9, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.KBUS, "D_STVL2", 0, 8),
                new EcuLogisticsEntry(174, "STVR", BusType.KBUS, "D_STVR2", 1, 8),
                new EcuLogisticsEntry(176, "RADIO", BusType.KBUS, "D_00b0", -1, -1),
                new EcuLogisticsEntry(185, "IRS", BusType.UNKNOWN, "D_00b9", -1, -1),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 0, 7),
                new EcuLogisticsEntry(194, "SVT", BusType.KBUS, "D_00c2", 1, 6),
                new EcuLogisticsEntry(200, "TELEFON", BusType.KBUS, "D_00c8", 4, 5),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 2, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.BYTEFLIGHT, "D_00ce", -1, -1),
                new EcuLogisticsEntry(208, "LM", BusType.FACAN, "D_00d0", 9, 8),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 0, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.UNKNOWN, "D_00e0", -1, -1),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 1, 3),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.UNKNOWN, "D_00ea", -1, -1),
                new EcuLogisticsEntry(237, "VIDEO", BusType.KBUS, "D_00ed", 4, 7),
                new EcuLogisticsEntry(240, "CIDB", BusType.KBUS, "D_00f0", 4, 6),
                new EcuLogisticsEntry(245, "LKM", BusType.KBUS, "D_00f5|d_szm", 4, 2)
            });
        public static EcuLogisticsData EcuLogisticsDataE85 =
            new EcuLogisticsData("E85EcuCharacteristics.xml", EcuLogisticsE85);

        // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.R50EcuCharacteristics.R50EcuCharacteristics
        public static ReadOnlyCollection<IEcuLogisticsEntry> EcuLogisticsR50 =
            new ReadOnlyCollection<IEcuLogisticsEntry>(new EcuLogisticsEntry[]
            {
                new EcuLogisticsEntry(128, "KOMBI", BusType.ROOT, "D_0080|D_KOMBI", 5, 0),
                new EcuLogisticsEntry(129, "RIP", BusType.KBUS, "D_0081", 5, 1),
                new EcuLogisticsEntry(0, "ZKE", BusType.KBUS, "D_0000|D_ZKE_GM", 3, 0),
                new EcuLogisticsEntry(41, "ABS/DSC", BusType.FACAN, "D_ABSKWP", 6, 1),
                new EcuLogisticsEntry(16, "DME", BusType.FACAN, "D_0010", 7, 1),
                new EcuLogisticsEntry(18, "DME", BusType.FACAN, "D_MOTOR|D_0012", 7, 1),
                new EcuLogisticsEntry(19, "DME", BusType.FACAN, "D_0013", 7, 1),
                new EcuLogisticsEntry(87, "LWS", BusType.FACAN, "D_0057", 6, 2),
                new EcuLogisticsEntry(49, "EHPS", BusType.FACAN, "D_0031", 7, 2),
                new EcuLogisticsEntry(24, "EGS", BusType.FACAN, "D_EGS", 7, 3),
                new EcuLogisticsEntry(154, "LWR", BusType.KBUS, "D_009a", 1, 1),
                new EcuLogisticsEntry(68, "EWS", BusType.KBUS, "D_0044", 2, 1),
                new EcuLogisticsEntry(96, "PDC", BusType.KBUS, "D_0060", 1, 2),
                new EcuLogisticsEntry(91, "IHKA", BusType.KBUS, "D_005B", 2, 2),
                new EcuLogisticsEntry(156, "CVM", BusType.KBUS, "D_009c", 1, 3),
                new EcuLogisticsEntry(155, "CVM", BusType.KBUS, "D_009b", 1, 3),
                new EcuLogisticsEntry(112, "RDC", BusType.KBUS, "D_0070", 1, 4),
                new EcuLogisticsEntry(164, "ZAE", BusType.KBUS, "D_00a4|D_SIM", 2, 4),
                new EcuLogisticsEntry(8, "SHD", BusType.KBUS, "D_0008", 1, 5),
                new EcuLogisticsEntry(232, "RLS", BusType.KBUS, "D_00e8", 1, 6),
                new EcuLogisticsEntry(116, "OC", BusType.KBUS, "D_0074", 2, 6),
                new EcuLogisticsEntry(240, "CIDB", BusType.KBUS, "D_00f0", 1, 7),
                new EcuLogisticsEntry(59, "VIDEO", BusType.KBUS, "D_003b", 2, 7),
                new EcuLogisticsEntry(237, "VIDEO", BusType.KBUS, "D_00ed", 2, 7),
                new EcuLogisticsEntry(118, "CDC", BusType.KBUS, "D_0076", 1, 8),
                new EcuLogisticsEntry(106, "DSP", BusType.KBUS, "D_006A", 2, 8),
                new EcuLogisticsEntry(104, "RADIO", BusType.KBUS, "D_0068", 1, 9),
                new EcuLogisticsEntry(80, "MFL", BusType.KBUS, "D_0050", 2, 9),
                new EcuLogisticsEntry(127, "NAVI", BusType.KBUS, "D_007f", 1, 10),
                new EcuLogisticsEntry(187, "JNAV", BusType.KBUS, "D_00bb", 1, 10),
                new EcuLogisticsEntry(200, "TELEFON", BusType.KBUS, "D_00c8", 2, 10),
                new EcuLogisticsEntry(13, "KOMBI", BusType.FACAN, "D_000D", -1, -1),
                new EcuLogisticsEntry(17, "ZKE", BusType.KBUS, "D_0011", 1, 2),
                new EcuLogisticsEntry(21, "DDSHD", BusType.KBUS, "D_0015", 1, 7),
                new EcuLogisticsEntry(22, "TOENS", BusType.KBUS, "D_0016", -1, -1),
                new EcuLogisticsEntry(25, "ATCU", BusType.KBUS, "D_0019", -1, -1),
                new EcuLogisticsEntry(32, "EML", BusType.KBUS, "D_0020", -1, -1),
                new EcuLogisticsEntry(33, "ZV", BusType.KBUS, "D_0021", -1, -1),
                new EcuLogisticsEntry(34, "EML", BusType.FACAN, "D_0022", 7, 3),
                new EcuLogisticsEntry(36, "HKM", BusType.KBUS, "D_0024", -1, -1),
                new EcuLogisticsEntry(40, "RCC", BusType.KBUS, "D_0028", -1, -1),
                new EcuLogisticsEntry(44, "VNC", BusType.KBUS, "D_002C", -1, -1),
                new EcuLogisticsEntry(46, "EDC", BusType.UNKNOWN, "D_002E", -1, -1),
                new EcuLogisticsEntry(48, "CCM", BusType.KBUS, "D_0030", 0, 5),
                new EcuLogisticsEntry(50, "EGS", BusType.FACAN, "D_0032", 7, 4),
                new EcuLogisticsEntry(52, "VGSG", BusType.FACAN, "D_VGSG", 7, 10),
                new EcuLogisticsEntry(53, "LSM/EPS", BusType.FACAN, "D_0035|D_EPS", 6, 1),
                new EcuLogisticsEntry(54, "ABS", BusType.FACAN, "D_0036", -1, -1),
                new EcuLogisticsEntry(64, "FBZV", BusType.KBUS, "D_0040", 0, 6),
                new EcuLogisticsEntry(69, "DWA", BusType.KBUS, "D_0045", -1, -1),
                new EcuLogisticsEntry(71, "CID", BusType.KBUS, "D_0047", -1, -1),
                new EcuLogisticsEntry(72, "JBIT", BusType.KBUS, "D_0048", -1, -1),
                new EcuLogisticsEntry(86, "DSC", BusType.FACAN, "D_0056", 7, 5),
                new EcuLogisticsEntry(89, "HKA", BusType.KBUS, "D_0059", -1, -1),
                new EcuLogisticsEntry(90, "ELV", BusType.KBUS, "D_005A", -1, -1),
                new EcuLogisticsEntry(101, "EKP", BusType.FACAN, "D_0065", 7, 11),
                new EcuLogisticsEntry(102, "ALC", BusType.FACAN, "D_0066", 7, 13),
                new EcuLogisticsEntry(105, "EKM", BusType.KBUS, "D_0069", -1, -1),
                new EcuLogisticsEntry(107, "STH", BusType.KBUS, "D_ZUHEIZ", 1, 5),
                new EcuLogisticsEntry(108, "EGS", BusType.KBUS, "D_006C", -1, -1),
                new EcuLogisticsEntry(113, "SM/SPM", BusType.KBUS, "D_0071", -1, -1),
                new EcuLogisticsEntry(114, "SM", BusType.KBUS, "D_0072|D_0072b", 0, 2),
                new EcuLogisticsEntry(134, "AHK", BusType.KBUS, "D_0086", -1, -1),
                new EcuLogisticsEntry(153, "NO_DME", BusType.VIRTUAL, "D_0099", -1, -1),
                new EcuLogisticsEntry(157, "ETS", BusType.KBUS, "D_009d", -1, -1),
                new EcuLogisticsEntry(158, "UEB", BusType.KBUS, "D_009e", 2, 7),
                new EcuLogisticsEntry(160, "FOND_ZIS", BusType.KBUS, "D_00a0", -1, -1),
                new EcuLogisticsEntry(161, "SBSL", BusType.SIBUS, "D_SBSL", -1, -1),
                new EcuLogisticsEntry(162, "SBSR", BusType.SIBUS, "D_SBSR", -1, -1),
                new EcuLogisticsEntry(166, "GR", BusType.KBUS, "D_00a6", -1, -1),
                new EcuLogisticsEntry(167, "FHK", BusType.KBUS, "D_00a7", -1, -1),
                new EcuLogisticsEntry(172, "EHC2", BusType.FACAN, "D_00AC", 7, 12),
                new EcuLogisticsEntry(173, "STVL", BusType.KBUS, "D_STVL", -1, -1),
                new EcuLogisticsEntry(174, "STVR", BusType.KBUS, "D_STVR", -1, -1),
                new EcuLogisticsEntry(176, "RADIO", BusType.KBUS, "D_00b0", -1, -1),
                new EcuLogisticsEntry(185, "IRS", BusType.KBUS, "D_00b9", -1, -1),
                new EcuLogisticsEntry(192, "ZIS", BusType.KBUS, "D_00c0", 0, 7),
                new EcuLogisticsEntry(194, "SVT", BusType.KBUS, "D_00c2", 1, 6),
                new EcuLogisticsEntry(205, "BC", BusType.KBUS, "D_00cd", 2, 9),
                new EcuLogisticsEntry(206, "RDC/SBE", BusType.KBUS, "D_00ce", -1, -1),
                new EcuLogisticsEntry(208, "LM", BusType.FACAN, "D_00d0", 7, 8),
                new EcuLogisticsEntry(218, "SM", BusType.KBUS, "D_00da", 0, 4),
                new EcuLogisticsEntry(224, "IRIS", BusType.KBUS, "D_00e0", -1, -1),
                new EcuLogisticsEntry(234, "DSP_BT", BusType.UNKNOWN, "D_00ea", -1, -1),
                new EcuLogisticsEntry(245, "LKM", BusType.KBUS, "D_00f5|d_szm", 4, 2)
            });
        public static EcuLogisticsData EcuLogisticsDataR50 =
            new EcuLogisticsData("R50EcuCharacteristics.xml", EcuLogisticsR50);

        public static ReadOnlyCollection<EcuLogisticsData> EcuLogisticsList = new ReadOnlyCollection<EcuLogisticsData>(new EcuLogisticsData[]
        {
            EcuLogisticsDataE36,
            EcuLogisticsDataE38,
            EcuLogisticsDataE39,
            EcuLogisticsDataE46,
            EcuLogisticsDataE52,
            EcuLogisticsDataE83,
            EcuLogisticsDataE85,
            EcuLogisticsDataR50,
        });

        public const string ResultUnknown = "UNBEK";

        // ReSharper restore CoVariantArrayConversion
        // ReSharper restore RedundantExplicitArrayCreation

#if Android
        private static Dictionary<string, string> _typeKeyDict;
#endif
        private static bool EcuLogisticsCreated;

        public static void CreateEcuLogistics(string resourcePath)
        {
            if (EcuLogisticsCreated)
            {
                return;
            }

            foreach (EcuLogisticsData ecuLogisticsData in EcuLogisticsList)
            {
                if (ecuLogisticsData.UsageEntry == null)
                {
                    string resourceName = resourcePath + ecuLogisticsData.XmlName;
                    ecuLogisticsData.UsageEntry = ReadEcuLogisticsXml(resourceName) ?? ecuLogisticsData.StaticEntry;
                }
            }

            EcuLogisticsCreated = true;
#if Android && DEBUG
            {
                foreach (EcuLogisticsData ecuLogisticsData in EcuLogisticsList)
                {
                    if (ecuLogisticsData.UsageEntry == ecuLogisticsData.StaticEntry)
                    {
                        Android.Util.Log.Debug("CreateEcuLogistics", "Reading XML failed: " + ecuLogisticsData.XmlName);
                    }
                    else if (ecuLogisticsData.UsageEntry.Count != ecuLogisticsData.StaticEntry.Count)
                    {
                        Android.Util.Log.Debug("CreateEcuLogistics", "Different Length: " + ecuLogisticsData.XmlName);
                    }
                    else
                    {
                        for (int i = 0; i < ecuLogisticsData.UsageEntry.Count; i++)
                        {
                            if (ecuLogisticsData.UsageEntry[i].Name != ecuLogisticsData.StaticEntry[i].Name ||
                                ecuLogisticsData.UsageEntry[i].GroupSgbd != ecuLogisticsData.StaticEntry[i].GroupSgbd)
                            {
                                Android.Util.Log.Debug("CreateEcuLogistics", "Different Data for: " + ecuLogisticsData.XmlName +
                                    " Name:" + ecuLogisticsData.UsageEntry[i].Name + ", " + ecuLogisticsData.StaticEntry[i].Name +
                                    " Group:" + ecuLogisticsData.UsageEntry[i].GroupSgbd + ", " + ecuLogisticsData.StaticEntry[i].GroupSgbd);
                            }
                        }
                    }
                }
            }
#endif
        }

        public static ReadOnlyCollection<IEcuLogisticsEntry> ReadEcuLogisticsXml(string resourceName)
        {
            try
            {
                List<IEcuLogisticsEntry> ecuLogisticsList = new List<IEcuLogisticsEntry>();
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    XDocument xmlDoc = XDocument.Load(stream);
                    if (xmlDoc.Root == null)
                    {
                        return null;
                    }
                    XNamespace ns = xmlDoc.Root.GetDefaultNamespace();
                    XElement logisticsList = xmlDoc.Root.Element(ns + "EcuLogisticsList");
                    if (logisticsList == null)
                    {
                        return null;
                    }

                    foreach (XElement ecuLogisticsNode in logisticsList.Elements(ns + "EcuLogisticsEntry"))
                    {
                        int diagAddress = 0;
                        string name = string.Empty;
                        BusType busType = BusType.ROOT;
                        string groupSgbd = string.Empty;
                        int column = 0;
                        int row = 0;

                        XAttribute diagAddrAttrib = ecuLogisticsNode.Attribute("DiagAddress");
                        if (diagAddrAttrib != null)
                        {
                            if (!Int32.TryParse(diagAddrAttrib.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out diagAddress))
                            {
                                diagAddress = 0;
                            }
                        }

                        XAttribute nameAttrib = ecuLogisticsNode.Attribute("Name");
                        if (nameAttrib != null)
                        {
                            name = nameAttrib.Value;
                        }

                        XElement busNode = ecuLogisticsNode.Element(ns + "Bus");
                        if (busNode != null)
                        {
                            if (!Enum.TryParse(busNode.Value, true, out busType))
                            {
                                busType = BusType.ROOT;
                            }
                        }

                        XElement groupSgbdNode = ecuLogisticsNode.Element(ns + "GroupSgbd");
                        if (groupSgbdNode != null)
                        {
                            groupSgbd = groupSgbdNode.Value;
                        }

                        XElement columnNode = ecuLogisticsNode.Element(ns + "Column");
                        if (columnNode != null)
                        {
                            if (!Int32.TryParse(columnNode.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out column))
                            {
                                column = 0;
                            }
                        }

                        XElement rowNode = ecuLogisticsNode.Element(ns + "Row");
                        if (rowNode != null)
                        {
                            if (!Int32.TryParse(rowNode.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out row))
                            {
                                row = 0;
                            }
                        }

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(groupSgbd))
                        {
                            ecuLogisticsList.Add(new EcuLogisticsEntry(diagAddress, name, busType, groupSgbd, column, row));
                        }
                    }
                }

                return new ReadOnlyCollection<IEcuLogisticsEntry>(ecuLogisticsList);
            }
            catch (Exception)
            {
                return null;
            }
        }

#if Android
        public static Dictionary<string, string> GetTypeKeyDict(EdiabasNet ediabas, string databaseDir)
        {
            ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Extract type key dict");

            try
            {
                Dictionary<string, string> typeKeyDict = new Dictionary<string, string>();
                ZipFile zf = null;
                try
                {
                    using (FileStream fs = File.OpenRead(Path.Combine(databaseDir, DatabaseFileName)))
                    {
                        zf = new ZipFile(fs);
                        foreach (ZipEntry zipEntry in zf)
                        {
                            if (!zipEntry.IsFile)
                            {
                                continue; // Ignore directories
                            }
                            if (string.Compare(zipEntry.Name, "typekeys.txt", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                Stream zipStream = zf.GetInputStream(zipEntry);
                                using (StreamReader sr = new StreamReader(zipStream))
                                {
                                    while (sr.Peek() >= 0)
                                    {
                                        string line = sr.ReadLine();
                                        if (line == null)
                                        {
                                            break;
                                        }
                                        string[] lineArray = line.Split(',');
                                        if (lineArray.Length == 2)
                                        {
                                            if (!typeKeyDict.ContainsKey(lineArray[0]))
                                            {
                                                typeKeyDict.Add(lineArray[0], lineArray[1]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Extract type key dict done");
                        return typeKeyDict;
                    }
                }
                finally
                {
                    if (zf != null)
                    {
                        zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                        zf.Close(); // Ensure we release resources
                    }
                }
            }
            catch (Exception ex)
            {
                ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Extract type key dict exception: {0}", EdiabasNet.GetExceptionText(ex));
                return null;
            }
        }
#endif

        public static int GetModelYearFromVin(string vin)
        {
            try
            {
                if (string.IsNullOrEmpty(vin) || vin.Length < 10)
                {
                    return -1;
                }

                char yearCode = vin.ToUpperInvariant()[9];
                if (yearCode == '0')
                {
                    return -1;
                }
                if (Int32.TryParse(yearCode.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out Int32 value))
                {
                    if (value >= 1 && value <= 0xF)
                    {
                        return value + 2000;
                    }
                }
                if (yearCode >= 'G' && yearCode <= 'Z')
                {
                    if (yearCode > 'P')
                    {
                        if (yearCode >= 'R')
                        {
                            if (yearCode <= 'T')
                            {
                                return yearCode + 1942;
                            }
                            if (yearCode >= 'V')
                            {
                                return yearCode + 1941;
                            }
                        }
                    }
                    else
                    {
                        if (yearCode == 'P')
                        {
                            return yearCode + 1943;
                        }
                        if (yearCode >= 'G')
                        {
                            if (yearCode <= 'H')
                            {
                                return yearCode + 1945;
                            }
                            if (yearCode >= 'J' && yearCode <= 'N')
                            {
                                return yearCode + 1944;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return -1;
        }

#if Android
        public static string GetTypeKeyFromVin(string vin, EdiabasNet ediabas, string databaseDir)
        {
            ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Type key from VIN: {0}", vin ?? "No VIN");
            if (vin == null)
            {
                return null;
            }
            string serialNumber;
            if (vin.Length == 7)
            {
                serialNumber = vin;
            }
            else if (vin.Length == 17)
            {
                serialNumber = vin.Substring(10, 7);
            }
            else
            {
                ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "VIN length invalid");
                return null;
            }

            try
            {
                ZipFile zf = null;
                try
                {
                    using (FileStream fs = File.OpenRead(Path.Combine(databaseDir, DatabaseFileName)))
                    {
                        zf = new ZipFile(fs);
                        foreach (ZipEntry zipEntry in zf)
                        {
                            if (!zipEntry.IsFile)
                            {
                                continue; // Ignore directories
                            }
                            if (string.Compare(zipEntry.Name, "vinranges.txt", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                Stream zipStream = zf.GetInputStream(zipEntry);
                                using (StreamReader sr = new StreamReader(zipStream))
                                {
                                    while (sr.Peek() >= 0)
                                    {
                                        string line = sr.ReadLine();
                                        if (line == null)
                                        {
                                            break;
                                        }
                                        string[] lineArray = line.Split(',');
                                        if (lineArray.Length == 3 &&
                                            lineArray[0].Length == 7 && lineArray[1].Length == 7)
                                        {
                                            if (string.Compare(serialNumber, lineArray[0], StringComparison.OrdinalIgnoreCase) >= 0 &&
                                                string.Compare(serialNumber, lineArray[1], StringComparison.OrdinalIgnoreCase) <= 0)
                                            {
                                                return lineArray[2];
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Type key not found in vin ranges");
                        return null;
                    }
                }
                finally
                {
                    if (zf != null)
                    {
                        zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                        zf.Close(); // Ensure we release resources
                    }
                }
            }
            catch (Exception ex)
            {
                ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Type key from VIN exception: {0}", EdiabasNet.GetExceptionText(ex));
                return null;
            }
        }

        public static IEcuLogisticsEntry GetEcuLogisticsByGroupName(ReadOnlyCollection<IEcuLogisticsEntry> ecuLogisticsList, string name)
        {
            string nameLower = name.ToLowerInvariant();
            foreach (IEcuLogisticsEntry entry in ecuLogisticsList)
            {
                if (entry.GroupSgbd.ToLowerInvariant().Contains(nameLower))
                {
                    return entry;
                }
            }
            return null;
        }

        public static string GetVehicleTypeFromVin(string vin, EdiabasNet ediabas, string databaseDir)
        {
            ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Vehicle type from VIN: {0}", vin ?? "No VIN");
            string typeKey = GetTypeKeyFromVin(vin, ediabas, databaseDir);
            if (typeKey == null)
            {
                return null;
            }
            if (_typeKeyDict == null)
            {
                _typeKeyDict = GetTypeKeyDict(ediabas, databaseDir);
            }
            if (_typeKeyDict == null)
            {
                ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "No type key dict present");
                return null;
            }
            if (!_typeKeyDict.TryGetValue(typeKey.ToUpperInvariant(), out string vehicleType))
            {
                ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Vehicle type not found");
                return null;
            }
            ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Vehicle type: {0}", vehicleType);
            return vehicleType;
        }

        public static ReadOnlyCollection<IEcuLogisticsEntry> GetEcuLogisticsFromVehicleType(string resourcePath, string vehicleType, EdiabasNet ediabas)
        {
            ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ECU logistics from vehicle type: {0}", vehicleType ?? "No type");
            if (vehicleType == null)
            {
                return null;
            }

            CreateEcuLogistics(typeof(BmwDeepObd.XmlToolActivity).Namespace + ".VehicleInfo.");

            // Mapping could be found in:
            // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.VehicleLogistics.GetCharacteristics(Vehicle vecInfo)
            switch (vehicleType.ToUpperInvariant())
            {
                case "E36":
                    return EcuLogisticsDataE36.UsageEntry;

                case "E38":
                    return EcuLogisticsDataE38.UsageEntry;

                case "E39":
                    return EcuLogisticsDataE39.UsageEntry;

                case "E46":
                    return EcuLogisticsDataE46.UsageEntry;

                case "E52":
                    return EcuLogisticsDataE52.UsageEntry;

                case "E53":
                    return EcuLogisticsDataE53.UsageEntry;

                case "E83":
                    return EcuLogisticsDataE83.UsageEntry;

                case "E85":
                case "E86":
                    return EcuLogisticsDataE85.UsageEntry;

                case "R50":
                case "R52":
                case "R53":
                    return EcuLogisticsDataR50.UsageEntry;
            }
            ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Vehicle type unknown");
            return null;
        }
#endif

        // from: RheingoldCoreFramework.dll BMW.Rheingold.CoreFramework.DatabaseProvider.FA.ExtractEreihe
        public static string GetVehicleTypeFromBrName(string brName, EdiabasNet ediabas)
        {
            ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Vehicle type from BR name: {0}", brName ?? "No name");
            if (brName == null)
            {
                return null;
            }
            if (string.Compare(brName, ResultUnknown, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return null;
            }
            if (brName.Length != 4)
            {
                return null;
            }
            if (brName.EndsWith("_", StringComparison.Ordinal))
            {
                string vehicleType = brName.TrimEnd('_');
                if (Regex.Match(vehicleType, "[ERKHM]\\d\\d").Success)
                {
                    return vehicleType;
                }
            }
            if (brName.StartsWith("RR", StringComparison.OrdinalIgnoreCase))
            {
                string vehicleType = brName.TrimEnd('_');
                if (Regex.Match(vehicleType, "^RR\\d$").Success)
                {
                    return vehicleType;
                }
                if (Regex.Match(vehicleType, "^RR0\\d$").Success)
                {
                    return "RR" + brName.Substring(3, 1);
                }
                if (Regex.Match(vehicleType, "^RR1\\d$").Success)
                {
                    return vehicleType;
                }
            }
            return brName.Substring(0, 1) + brName.Substring(2, 2);
        }

        public static string GetGroupSgbdFromVehicleType(string vehicleType, string vin, DateTime? cDate, EdiabasNet ediabas, out BnType bnType)
        {
            bnType = BnType.UNKNOWN;
            string cDateStr = "No date";
            if (cDate.HasValue)
            {
                cDateStr = cDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Group SGBD from vehicle type: {0}, VIN: {1}, CDate: {2}", vehicleType ?? "No type", vin ?? "No VIN", cDateStr);
            if (vehicleType == null)
            {
                return null;
            }

            string vinTypeUpper = string.Empty;
            if (!string.IsNullOrEmpty(vin) && vin.Length >= 17)
            {
                vinTypeUpper = vin.Substring(3, 4).ToUpperInvariant();
            }

            // Mapping could be found in:
            // from: RheingoldDiagnostics.dll: BMW.Rheingold.Diagnostics.VehicleLogistics.GetCharacteristics(Vehicle vecInfo)
            string typeUpper = vehicleType.ToUpperInvariant();
            switch (typeUpper)
            {
                case "E60": // E60EcuCharacteristics
                case "E61": // E60EcuCharacteristics
                case "E62": // BNType: UNKNOWN
                case "E63": // E60EcuCharacteristics
                case "E64": // E60EcuCharacteristics
                    bnType = BnType.UNKNOWN;
                    return "e60";

                case "E65": // E65EcuCharacteristics
                case "E66": // E65EcuCharacteristics
                case "E67": // E65EcuCharacteristics
                case "E68": // E65EcuCharacteristics
                    bnType = BnType.UNKNOWN;
                    return "e65";

                case "E70": // E70EcuCharacteristics
                case "E71": // E70EcuCharacteristics, E70EcuCharacteristicsAMPT, E70EcuCharacteristicsAMPH: MainSeriesSgbd="e70"
                case "E72": // E72EcuCharacteristics
                    bnType = BnType.UNKNOWN;
                    return "e70";

                case "M12": // E89XEcuCharacteristics BEV2010
                    bnType = BnType.BEV2010;
                    return "e89x";

                case "E89X": // E89XEcuCharacteristics
                case "E81": // E89XEcuCharacteristics
                case "E82": // E89XEcuCharacteristics
                case "E84": // E89XEcuCharacteristics
                case "E87": // E89XEcuCharacteristics
                case "E88": // E89XEcuCharacteristics
                case "E89": // E89XEcuCharacteristics
                case "E90": // E89XEcuCharacteristics
                case "E91": // E89XEcuCharacteristics
                case "E92": // E89XEcuCharacteristics
                case "E93": // E89XEcuCharacteristics
                    bnType = BnType.IBUS;
                    // E89XEcuCharacteristics -> E89EcuCharacteristics
                    return "e89x";

                case "M13": // F56EcuCharacteristics
                    bnType = BnType.BN2020;
                    return "f01";

                case "E38": // E38EcuCharacteristics
                case "E46": // E46EcuCharacteristics
                case "E83": // E83EcuCharacteristics
                case "E85": // E85EcuCharacteristics
                    bnType = BnType.IBUS;
                    // return "zcs_all";
                    break;

                case "E36": // E36EcuCharacteristics
                    bnType = BnType.IBUS;
                    // return "zcs_e36";
                    break;

                case "E39": // E39EcuCharacteristics
                    bnType = BnType.IBUS;
                    // return "zcs_e39";
                    break;

                case "E52": // E52EcuCharacteristics
                    bnType = BnType.IBUS;
                    // return "zcs_e52";
                    break;

                case "E53": // E53EcuCharacteristics
                    bnType = BnType.IBUS;
                    // return "zcs_e53";
                    break;

                case "H61": // H61EcuCharacteristics
                case "H91": // H61EcuCharacteristics
                    bnType = BnType.BN2020_MOTORBIKE;
                    return "MRKH24";

                case "247": // MRK01XEcuCharacteristics
                case "247E": // MRK01XEcuCharacteristics
                case "248": // MRK01XEcuCharacteristics
                case "259": // MRK01XEcuCharacteristics
                case "259C": // MRK01XEcuCharacteristics
                case "259E": // MRK01XEcuCharacteristics
                case "259R": // MRK01XEcuCharacteristics
                case "259S": // MRK01XEcuCharacteristics
                case "C01": // MRK01XEcuCharacteristics
                case "E169": // MRK01XEcuCharacteristics
                case "E189": // MRK01XEcuCharacteristics
                case "K14": // MRK01XEcuCharacteristics
                case "K15": // MRK01XEcuCharacteristics
                case "K16": // MRK01XEcuCharacteristics
                case "K17": // MRK01XEcuCharacteristics
                case "K30": // MRK01XEcuCharacteristics
                case "K41": // MRK01XEcuCharacteristics
                case "K569": // MRK01XEcuCharacteristics
                case "K589": // MRK01XEcuCharacteristics
                case "K599": // MRK01XEcuCharacteristics
                case "R13": // MRK01XEcuCharacteristics
                case "R21": // MRK01XEcuCharacteristics
                case "R22": // MRK01XEcuCharacteristics
                case "R28": // MRK01XEcuCharacteristics
                    bnType = BnType.BNK01X_MOTORBIKE;
                    return "MRK24";

                case "GT1": // GibbsEcuCharacteristics, BNType: BN2000_GIBBS
                    bnType = BnType.BN2000_GIBBS;
                    return "MRK24";

                case "A67": // MREcuCharacteristics
                case "K25": // MREcuCharacteristics
                case "K26": // MREcuCharacteristics
                case "K27": // MREcuCharacteristics
                case "K28": // MREcuCharacteristics
                case "K29": // MREcuCharacteristics
                case "K40": // MREcuCharacteristics
                case "K42": // MREcuCharacteristics
                case "K43": // MREcuCharacteristics
                case "K44": // MREcuCharacteristics
                case "K70": // MREcuCharacteristics
                case "K71": // MREcuCharacteristics
                case "K72": // MREcuCharacteristics
                case "K73": // MREcuCharacteristics
                case "K75": // MREcuCharacteristics
                case "MRK24": // MREcuCharacteristics
                case "V98": // MREcuCharacteristics
                    bnType = BnType.BN2000_MOTORBIKE;
                    return "MRK24";

                case "K18":
                    if (!string.IsNullOrEmpty(vin) &&
                        (vinTypeUpper.Equals("0C04", StringComparison.OrdinalIgnoreCase) || vinTypeUpper.Equals("0C14", StringComparison.OrdinalIgnoreCase)))
                    {
                        bnType = BnType.BN2020;
                        // BN2020: MRXEcuCharacteristics
                        return "x_k001";
                    }
                    bnType = BnType.BN2000;
                    // BN2000: MREcuCharacteristics
                    return "MRK24";

                case "K19":
                    if (!string.IsNullOrEmpty(vin) &&
                        (vinTypeUpper.Equals("0C05", StringComparison.OrdinalIgnoreCase) || vinTypeUpper.Equals("0C15", StringComparison.OrdinalIgnoreCase)))
                    {
                        bnType = BnType.BN2020;
                        // BN2020: MRXEcuCharacteristics
                        return "x_k001";
                    }
                    bnType = BnType.BN2000;
                    // BN2000: MREcuCharacteristics
                    return "MRK24";

                case "K21":
                    if (!string.IsNullOrEmpty(vin) &&
                        (vinTypeUpper.Equals("0A06", StringComparison.OrdinalIgnoreCase) || vinTypeUpper.Equals("0A16", StringComparison.OrdinalIgnoreCase)))
                    {
                        bnType = BnType.BN2000;
                        // BN2000: MREcuCharacteristics
                        return "MRK24";
                    }
                    bnType = BnType.BN2020;
                    // BN2020: MRXEcuCharacteristics
                    return "x_k001";

                case "K46":
                    if (!string.IsNullOrEmpty(vin) && !vinTypeUpper.Equals("XXXX", StringComparison.OrdinalIgnoreCase) &&
                        (vinTypeUpper.Equals("0D10", StringComparison.OrdinalIgnoreCase) || vinTypeUpper.Equals("0D21", StringComparison.OrdinalIgnoreCase) ||
                         vinTypeUpper.Equals("0D30", StringComparison.OrdinalIgnoreCase) || vinTypeUpper.Equals("0D40", StringComparison.OrdinalIgnoreCase) ||
                         vinTypeUpper.Equals("0D50", StringComparison.OrdinalIgnoreCase) || vinTypeUpper.Equals("0D60", StringComparison.OrdinalIgnoreCase) ||
                         vinTypeUpper.Equals("0D70", StringComparison.OrdinalIgnoreCase) || vinTypeUpper.Equals("0D80", StringComparison.OrdinalIgnoreCase) ||
                         vinTypeUpper.Equals("0D90", StringComparison.OrdinalIgnoreCase)
                         ))
                    {
                        bnType = BnType.BN2020_MOTORBIKE;
                        // BN2020_MOTORBIKE: MRXEcuCharacteristics
                        return "x_k001";
                    }
                    bnType = BnType.BN2000_MOTORBIKE;
                    // BN2000_MOTORBIKE: MREcuCharacteristics
                    return "MRK24";

                case "K22": // MRXEcuCharacteristics
                case "K23": // MRXEcuCharacteristics
                case "K32": // MRXEcuCharacteristics
                case "K33": // MRXEcuCharacteristics
                case "K47": // MRXEcuCharacteristics
                case "K48": // MRXEcuCharacteristics
                case "K49": // MRXEcuCharacteristics
                case "K50": // MRXEcuCharacteristics
                case "K51": // MRXEcuCharacteristics
                case "K52": // MRXEcuCharacteristics
                case "K53": // MRXEcuCharacteristics
                case "K54": // MRXEcuCharacteristics
                case "K61": // MRXEcuCharacteristics
                case "K67": // MRXEcuCharacteristics
                case "K69": // MRXEcuCharacteristics
                case "K80": // MRXEcuCharacteristics
                case "K81": // MRXEcuCharacteristics
                case "K82": // MRXEcuCharacteristics
                case "K83": // MRXEcuCharacteristics
                case "K84": // MRXEcuCharacteristics
                case "V99": // MRXEcuCharacteristics
                case "X_K001": // MRXEcuCharacteristics
                    bnType = BnType.BN2020_MOTORBIKE;
                    return "x_k001";

                case "K02": // MRXEcuCharacteristics
                case "K03": // MRXEcuCharacteristics
                case "K08": // MRXEcuCharacteristics
                case "K09": // MRXEcuCharacteristics
                case "K60": // MRXEcuCharacteristics
                    // special case in MRXEcuCharacteristics
                    bnType = BnType.BN2020_MOTORBIKE;
                    return "x_ks01";

                case "RR1": // RREcuCharacteristics, RR2EcuCharacteristics
                case "RR2": // RREcuCharacteristics, RR2EcuCharacteristics
                case "RR3": // RREcuCharacteristics, RR2EcuCharacteristics
                    bnType = BnType.UNKNOWN;
                    if (cDate.HasValue && !(cDate.Value < new DateTime(2012, 06, 01)))
                    {
                        // RR2EcuCharacteristics
                        return "rr1_2020";
                    }
                    // RREcuCharacteristics
                    return "rr1";

                case "R55": // R55EcuCharacteristics
                case "R56": // R55EcuCharacteristics
                case "R57": // R55EcuCharacteristics
                case "R58": // R55EcuCharacteristics
                case "R59": // R55EcuCharacteristics
                case "R60": // R55EcuCharacteristics
                case "R61": // R55EcuCharacteristics
                    bnType = BnType.BN2000;
                    return "r56";

                case "R50": // R50EcuCharacteristics
                case "R52": // R50EcuCharacteristics
                case "R53": // R50EcuCharacteristics
                    bnType = BnType.IBUS;
                    // return "zcs_all";
                    return null;

                case "RR11": // BNT_RR1X_RR3X_RRNM
                case "RR12": // BNT_RR1X_RR3X_RRNM
                case "RR21": // BNT_RR1X_RR3X_RRNM
                case "RR22": // BNT_RR1X_RR3X_RRNM
                case "RR31": // BNT_RR1X_RR3X_RRNM
                case "RR4": // RR6EcuCharacteristics
                case "RR5": // RR6EcuCharacteristics
                case "RR6": // RR6EcuCharacteristics
                    bnType = BnType.BN2020;
                    return "f01";
            }

            if (typeUpper.StartsWith("F") || typeUpper.StartsWith("G") || typeUpper.StartsWith("I") || typeUpper.StartsWith("J") || typeUpper.StartsWith("U"))
            {
                // F01, F02, F03, F04, F06, F07, F10, F11, F12, F13, F18: F01EcuCharacteristics, F01_1307EcuCharacteristics
                // F39, F54, F55, F56, F60: F56EcuCharacteristics
                // F20, F21, F22, F23, F30, F31, F32, F33, F34, F35, F36, F80, F81, F82, F83, F87: F20EcuCharacteristics
                // F25, F26: F25EcuCharacteristics, F25_1404EcuCharacteristics
                // F14, F15, F16, F85, F86: F15EcuCharacteristics
                // F40, F44: BNT_F40_F44
                // F45, F46, F47, F48, F49, F52, F57: F56EcuCharacteristics
                // F90, G30, G31, G32, G38: BNT_G11_G12_G3X_SP2015
                // F91, F92, F93, G14, G15, G16: BNT_G1X_G3X_SP2018, BNT_G1X_G3X_SP2018_MGU, BNT_G1X_G3X_SP2018_noMGU
                // F95, F96, G05, G06, G07: BNT_G05_G06_G07
                // F97, F98, G01, G02, G08: BNT_G01_G02_G08_F97_F98_SP2015
                // G11, G12: BNT_G11_G12_G3X_SP2015, BNT_G1X_G3X_SP2018_MGU, BNT_G1X_G3X_SP2018_noMGU, BNT_G1X_G3X_SP2018
                // G20, G21, G22, G23, G26, G28: BNT_G20_G28
                // G29: BNT_G29
                // I01, F01BN2K: I01EcuCharacteristics
                // I12, I15: BNT_I12_I15
                // I20: BNT_I20, (MRR_30: BNT-XML-I20_FRS), (FRR_30V: BNT-XML-I20_FRSF)
                // J29: BNT_J29
                // U10, U11, U12: BNT_U06...
                // U06: BNT-XML-U06...
                bnType = BnType.BN2020;
                return "f01";
            }
            ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Vehicle type unknown");
            return null;
        }
    }
}

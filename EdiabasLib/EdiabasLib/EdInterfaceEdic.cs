﻿using System;
// ReSharper disable ConvertPropertyToExpressionBody

namespace EdiabasLib
{
    public class EdInterfaceEdic : EdInterfaceObd
    {
        public override EdiabasNet Ediabas
        {
            get
            {
                return base.Ediabas;
            }
            set
            {
                base.Ediabas = value;

                string prop = EdiabasProtected.GetConfigProperty("EdicComPort");
                if (prop != null)
                {
                    ComPortProtected = prop;
                }
            }
        }

        public override string InterfaceType
        {
            get
            {
                return "EDIC";
            }
        }

        public override UInt32 InterfaceVersion
        {
            get
            {
                return 63;
            }
        }

        public override string InterfaceName
        {
            get
            {
                return "EDIC";
            }
        }

        public override bool IsValidInterfaceName(string name)
        {
            return IsValidInterfaceNameStatic(name);
        }

        public new static bool IsValidInterfaceNameStatic(string name)
        {
            string[] nameParts = name.Split(':');
            if (nameParts.Length > 0 && string.Compare(nameParts[0], "EDIC", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            return false;
        }

        protected override bool EdicSimulation
        {
            get
            {
                return true;
            }
        }
    }
}

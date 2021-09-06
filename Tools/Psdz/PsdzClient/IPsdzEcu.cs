﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PsdzClient
{
    public interface IPsdzEcu
    {
        string BaseVariant { get; }

        string BnTnName { get; }

        IEnumerable<PsdzBus> BusConnections { get; }

        PsdzBus DiagnosticBus { get; }

        IPsdzEcuDetailInfo EcuDetailInfo { get; }

        IPsdzEcuStatusInfo EcuStatusInfo { get; }

        string EcuVariant { get; }

        IPsdzDiagAddress GatewayDiagAddr { get; }

        IPsdzEcuIdentifier PrimaryKey { get; }

        string SerialNumber { get; }

        IPsdzStandardSvk StandardSvk { get; }

        IPsdzEcuPdxInfo PsdzEcuPdxInfo { get; }
    }
}

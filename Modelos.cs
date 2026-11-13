using System.Collections.Generic;

namespace GeneradorNominaXml
{
    public class LayoutData
    {
        public Emisor Emisor { get; set; } = new Emisor();
        public CfdConfig Cfd { get; set; } = new CfdConfig();
        public Dictionary<string, NominaMaestra> Nominas { get; set; } = new Dictionary<string, NominaMaestra>();
        public List<EmpleadoData> Empleados { get; set; } = new List<EmpleadoData>();
    }

    public class Emisor
    {
        public string Rfc { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string RegimenFiscal { get; set; } = "";
    }

    public class CfdConfig
    {
        public string Serie { get; set; } = "";
        public string FolioInicial { get; set; } = "1";
        public string LugarExpedicion { get; set; } = "";
        public string Fecha { get; set; } = "";
    }

    public class NominaMaestra
    {
        public string Id { get; set; } = "";
        public string RegistroPatronal { get; set; } = "";
        public string FechaPago { get; set; } = "";
        public string FechaInicialPago { get; set; } = "";
        public string FechaFinalPago { get; set; } = "";
        public string TotalPercepciones { get; set; } = "0.00";
        public string TotalDeducciones { get; set; } = "0.00";
        public string NumDiasPagados { get; set; } = "1";
        public string TotalOtrosPagos { get; set; } = "0.00";
        public string TipoNomina { get; set; } = "E";
    }

    public class EmpleadoData
    {
        public string IdNomina { get; set; } = "";
        public string NumEmpleado { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Rfc { get; set; } = "";
        public string Curp { get; set; } = "";
        public string TipoRegimen { get; set; } = "";
        public string NumSeguridadSocial { get; set; } = "";
        public string RiesgoPuesto { get; set; } = "";
        public string FechaInicioRelLaboral { get; set; } = "";
        public string TipoContrato { get; set; } = "";
        public string TipoJornada { get; set; } = "";
        public string PeriodicidadPago { get; set; } = "";
        public string SalarioDiarioIntegrado { get; set; } = "";
        public string Sindicalizado { get; set; } = "";
        public string ClaveEntFed { get; set; } = "";
        public string DomicilioFiscalReceptor { get; set; } = "";
        public string RegistroPatronal { get; set; } = "";
        public string EntidadSNCF { get; set; } = "";
        public List<Percepcion> Percepciones { get; set; } = new List<Percepcion>();
        public List<OtroPago> OtrosPagos { get; set; } = new List<OtroPago>();
    }

    public class Percepcion
    {
        public string TipoPercepcion { get; set; } = "";
        public string Clave { get; set; } = "";
        public string Concepto { get; set; } = "";
        public decimal ImporteGravado { get; set; }
        public decimal ImporteExento { get; set; }
    }

    public class OtroPago
    {
        public string TipoOtroPago { get; set; } = "";
        public string Clave { get; set; } = "";
        public string Concepto { get; set; } = "";
        public decimal Importe { get; set; }
        public decimal SubsidioCausado { get; set; }
    }
}

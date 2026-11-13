using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GeneradorNominaXml
{
    public class NominaXmlGenerator
    {
        public int GenerarArchivosXml(string txtPath, string outputDir, int folioInicial, IProgress<ProgresoGeneracion> progress)
        {
            if (string.IsNullOrWhiteSpace(txtPath) || !File.Exists(txtPath))
                throw new FileNotFoundException("No se encontró el archivo TXT seleccionado.", txtPath);
            if (string.IsNullOrWhiteSpace(outputDir))
                throw new ArgumentException("Debe indicar una carpeta de salida.", "outputDir");

            Directory.CreateDirectory(outputDir);
            LayoutData layout = ParseTxt(txtPath);
            if (layout.Empleados.Count == 0)
                throw new InvalidDataException("El archivo TXT no contiene empleados (registros E02).");

            XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";
            XNamespace nomina12 = "http://www.sat.gob.mx/nomina12";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            int contador = 0;
            string marcaTiempo = DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);

            foreach (EmpleadoData emp in layout.Empleados)
            {
                contador++;
                int folioActual = folioInicial + contador - 1;
                NominaMaestra nmm;
                if (!layout.Nominas.TryGetValue(emp.IdNomina, out nmm))
                    throw new InvalidDataException("No existe registro NMM para la nómina " + emp.IdNomina + ". Empleado: " + emp.NumEmpleado);

                decimal totalPercepciones = emp.Percepciones.Sum(p => p.ImporteGravado + p.ImporteExento);
                decimal totalOtrosPagos = emp.OtrosPagos.Sum(o => o.Importe);
                decimal totalComprobante = totalPercepciones + totalOtrosPagos;
                string antiguedadIso = CalcularAntiguedadSat(emp.FechaInicioRelLaboral, nmm.FechaFinalPago);
                string importe = totalComprobante.ToString("F2", CultureInfo.InvariantCulture);

                XElement nodoEmisorNomina = new XElement(nomina12 + "Emisor",
                    AtributoOpcional("RegistroPatronal", emp.RegistroPatronal),
                    string.IsNullOrWhiteSpace(emp.EntidadSNCF) ? null : new XElement(nomina12 + "EntidadSNCF", new XAttribute("OrigenRecurso", emp.EntidadSNCF)));

                XElement nodoPercepciones = new XElement(nomina12 + "Percepciones",
                    new XAttribute("TotalSueldos", importe),
                    new XAttribute("TotalGravado", emp.Percepciones.Sum(p => p.ImporteGravado).ToString("F2", CultureInfo.InvariantCulture)),
                    new XAttribute("TotalExento", emp.Percepciones.Sum(p => p.ImporteExento).ToString("F2", CultureInfo.InvariantCulture)),
                    emp.Percepciones.Select(p => new XElement(nomina12 + "Percepcion",
                        new XAttribute("TipoPercepcion", p.TipoPercepcion), new XAttribute("Clave", p.Clave),
                        new XAttribute("Concepto", p.Concepto),
                        new XAttribute("ImporteGravado", p.ImporteGravado.ToString("F2", CultureInfo.InvariantCulture)),
                        new XAttribute("ImporteExento", p.ImporteExento.ToString("F2", CultureInfo.InvariantCulture)))));

                XElement nodoOtrosPagos = emp.OtrosPagos.Any()
                    ? new XElement(nomina12 + "OtrosPagos", emp.OtrosPagos.Select(o => new XElement(nomina12 + "OtroPago",
                        new XAttribute("TipoOtroPago", o.TipoOtroPago), new XAttribute("Clave", o.Clave),
                        new XAttribute("Concepto", o.Concepto), new XAttribute("Importe", o.Importe.ToString("F2", CultureInfo.InvariantCulture)),
                        new XElement(nomina12 + "SubsidioAlEmpleo", new XAttribute("SubsidioCausado", o.SubsidioCausado.ToString("F2", CultureInfo.InvariantCulture))))))
                    : null;

                XElement comprobante = new XElement(cfdi + "Comprobante",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "cfdi", cfdi.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "nomina12", nomina12.NamespaceName),
                    new XAttribute(xsi + "schemaLocation", "http://www.sat.gob.mx/cfd/4 http://www.sat.gob.mx/sitio_internet/cfd/4/cfdv40.xsd " +
                        "http://www.sat.gob.mx/nomina12 http://www.sat.gob.mx/sitio_internet/cfd/nomina/nomina12.xsd"),
                    new XAttribute("Version", "4.0"), new XAttribute("TipoDeComprobante", "N"),
                    new XAttribute("Serie", layout.Cfd.Serie), new XAttribute("Folio", folioActual.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("Fecha", layout.Cfd.Fecha), new XAttribute("MetodoPago", "PUE"),
                    new XAttribute("SubTotal", importe), new XAttribute("Total", importe), new XAttribute("Moneda", "MXN"),
                    new XAttribute("LugarExpedicion", layout.Cfd.LugarExpedicion), new XAttribute("Exportacion", "01"),
                    new XElement(cfdi + "Emisor", new XAttribute("Rfc", layout.Emisor.Rfc), new XAttribute("Nombre", layout.Emisor.Nombre), new XAttribute("RegimenFiscal", layout.Emisor.RegimenFiscal)),
                    new XElement(cfdi + "Receptor", new XAttribute("Rfc", emp.Rfc), new XAttribute("Nombre", emp.Nombre),
                        new XAttribute("UsoCFDI", "CN01"), new XAttribute("DomicilioFiscalReceptor", emp.DomicilioFiscalReceptor), new XAttribute("RegimenFiscalReceptor", "605")),
                    new XElement(cfdi + "Conceptos", new XElement(cfdi + "Concepto", new XAttribute("Cantidad", "1"),
                        new XAttribute("ClaveUnidad", "ACT"), new XAttribute("ObjetoImp", "01"), new XAttribute("ClaveProdServ", "84111505"),
                        new XAttribute("Descripcion", "Pago de nómina"), new XAttribute("ValorUnitario", importe), new XAttribute("Importe", importe))),
                    new XElement(cfdi + "Complemento", new XElement(nomina12 + "Nomina",
                        new XAttribute("Version", "1.2"), new XAttribute("TipoNomina", nmm.TipoNomina),
                        new XAttribute("FechaPago", nmm.FechaPago), new XAttribute("FechaInicialPago", nmm.FechaInicialPago),
                        new XAttribute("FechaFinalPago", nmm.FechaFinalPago), new XAttribute("NumDiasPagados", nmm.NumDiasPagados),
                        AtributoOpcional("TotalPercepciones", totalPercepciones.ToString("F2", CultureInfo.InvariantCulture)),
                        AtributoOpcional("TotalOtrosPagos", totalOtrosPagos.ToString("F2", CultureInfo.InvariantCulture)),
                        nodoEmisorNomina,
                        new XElement(nomina12 + "Receptor", new XAttribute("Curp", emp.Curp),
                            AtributoOpcional("NumSeguridadSocial", emp.NumSeguridadSocial), AtributoOpcional("FechaInicioRelLaboral", emp.FechaInicioRelLaboral),
                            AtributoOpcional("Antigüedad", antiguedadIso), new XAttribute("TipoContrato", emp.TipoContrato),
                            new XAttribute("Sindicalizado", string.Equals(emp.Sindicalizado, "SI", StringComparison.OrdinalIgnoreCase) ? "Sí" : "No"),
                            new XAttribute("TipoJornada", emp.TipoJornada), new XAttribute("TipoRegimen", emp.TipoRegimen),
                            new XAttribute("NumEmpleado", emp.NumEmpleado), new XAttribute("RiesgoPuesto", emp.RiesgoPuesto),
                            new XAttribute("PeriodicidadPago", emp.PeriodicidadPago), new XAttribute("SalarioDiarioIntegrado", emp.SalarioDiarioIntegrado),
                            new XAttribute("ClaveEntFed", emp.ClaveEntFed)),
                        nodoPercepciones, nodoOtrosPagos)));

                XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", null), comprobante);
                string safeNumEmpleado = LimpiarNombreArchivo(emp.NumEmpleado);
                string fileName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}.xml", Path.GetFileName(txtPath), marcaTiempo, safeNumEmpleado, contador);
                string fullPath = Path.Combine(outputDir, fileName);
                XmlWriterSettings settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, OmitXmlDeclaration = false };
                using (XmlWriter writer = XmlWriter.Create(fullPath, settings)) doc.Save(writer);

                if (progress != null)
                    progress.Report(new ProgresoGeneracion { Actual = contador, Total = layout.Empleados.Count, Empleado = emp.NumEmpleado, Archivo = fileName });
            }
            return contador;
        }

        private static XAttribute AtributoOpcional(string nombre, string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : new XAttribute(nombre, valor);
        }

        private static string LimpiarNombreArchivo(string valor)
        {
            string resultado = string.IsNullOrWhiteSpace(valor) ? "SIN_NUMERO" : valor;
            foreach (char c in Path.GetInvalidFileNameChars()) resultado = resultado.Replace(c, '_');
            return resultado;
        }

        public static string CalcularAntiguedadSat(string fechaInicioStr, string fechaFinStr)
        {
            DateTime inicio, fin;
            if (string.IsNullOrWhiteSpace(fechaInicioStr) || string.IsNullOrWhiteSpace(fechaFinStr)) return "";
            if (!DateTime.TryParse(fechaInicioStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out inicio) &&
                !DateTime.TryParse(fechaInicioStr, out inicio)) return "";
            if (!DateTime.TryParse(fechaFinStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out fin) &&
                !DateTime.TryParse(fechaFinStr, out fin)) return "";

            int years = fin.Year - inicio.Year;
            int months = fin.Month - inicio.Month;
            int days = fin.Day - inicio.Day + 1;
            if (days < 0)
            {
                months--;
                DateTime mesAnterior = fin.AddMonths(-1);
                days += DateTime.DaysInMonth(mesAnterior.Year, mesAnterior.Month);
            }
            if (months < 0) { years--; months += 12; }
            return string.Format(CultureInfo.InvariantCulture, "P{0}Y{1}M{2}D", years, months, days);
        }

        public static LayoutData ParseTxt(string path)
        {
            LayoutData data = new LayoutData();
            Dictionary<string, EmpleadoData> empleadosMap = new Dictionary<string, EmpleadoData>();
            EmpleadoData currentEmp = null;
            int numeroLinea = 0;
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                numeroLinea++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] p = line.Split('|');
                string tag = p[0].Trim();
                try
                {
                    switch (tag)
                    {
                        case "EMP": ExigirCampos(p, 4, tag); data.Emisor.Rfc = p[1]; data.Emisor.Nombre = p[2]; data.Emisor.RegimenFiscal = p[3]; break;
                        case "CFD": ExigirCampos(p, 5, tag); data.Cfd.Serie = p[1]; data.Cfd.FolioInicial = p[2]; data.Cfd.LugarExpedicion = p[3]; data.Cfd.Fecha = p[4]; break;
                        case "NMM":
                            ExigirCampos(p, 11, tag);
                            NominaMaestra nmm = new NominaMaestra { Id = p[1], RegistroPatronal = p[2], FechaPago = p[3], FechaInicialPago = p[4], FechaFinalPago = p[5], TotalPercepciones = p[6], TotalDeducciones = p[7], NumDiasPagados = p[8], TotalOtrosPagos = p[9], TipoNomina = p[10] };
                            data.Nominas[nmm.Id] = nmm; break;
                        case "E02":
                            ExigirCampos(p, 28, tag);
                            string keyEmp = p[1] + "_" + p[2];
                            if (!empleadosMap.TryGetValue(keyEmp, out currentEmp))
                            {
                                currentEmp = new EmpleadoData { IdNomina = p[1], NumEmpleado = p[2], Nombre = p[3], Rfc = p[4], Curp = p[5], TipoRegimen = p[6], NumSeguridadSocial = p[7], RiesgoPuesto = p[8], FechaInicioRelLaboral = p[12], TipoContrato = p[15], TipoJornada = p[16], PeriodicidadPago = p[17], SalarioDiarioIntegrado = p[20], Sindicalizado = p[21], ClaveEntFed = p[22], DomicilioFiscalReceptor = p[27] };
                                empleadosMap[keyEmp] = currentEmp; data.Empleados.Add(currentEmp);
                            }
                            break;
                        case "RPT": ExigirCampos(p, 2, tag); if (currentEmp != null) currentEmp.RegistroPatronal = p[1]; break;
                        case "SNC": ExigirCampos(p, 2, tag); if (currentEmp != null) currentEmp.EntidadSNCF = p[1]; break;
                        case "OTP":
                            ExigirCampos(p, 6, tag);
                            if (currentEmp != null) currentEmp.OtrosPagos.Add(new OtroPago { TipoOtroPago = p[1], Clave = p[2], Concepto = p[3], Importe = ParseDecimal(p[4]), SubsidioCausado = ParseDecimal(p[5]) });
                            break;
                        case "P01":
                            ExigirCampos(p, 6, tag);
                            if (currentEmp != null) currentEmp.Percepciones.Add(new Percepcion { TipoPercepcion = p[1], Clave = p[2], Concepto = p[3], ImporteGravado = ParseDecimal(p[4]), ImporteExento = ParseDecimal(p[5]) });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("Error en la línea " + numeroLinea + " (registro " + tag + "): " + ex.Message, ex);
                }
            }
            if (string.IsNullOrWhiteSpace(data.Emisor.Rfc)) throw new InvalidDataException("No se encontró un registro EMP válido.");
            if (string.IsNullOrWhiteSpace(data.Cfd.Fecha)) throw new InvalidDataException("No se encontró un registro CFD válido.");
            return data;
        }

        private static void ExigirCampos(string[] campos, int minimo, string tag)
        {
            if (campos.Length < minimo) throw new InvalidDataException("El registro " + tag + " requiere al menos " + minimo + " campos y contiene " + campos.Length + ".");
        }

        private static decimal ParseDecimal(string valor)
        {
            decimal resultado;
            return decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado) ? resultado : 0m;
        }
    }

    public class ProgresoGeneracion
    {
        public int Actual { get; set; }
        public int Total { get; set; }
        public string Empleado { get; set; }
        public string Archivo { get; set; }
    }
}

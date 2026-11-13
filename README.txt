GENERADOR XML DE NOMINA - WINDOWS FORMS .NET FRAMEWORK 4.8

CONTENIDO
- GeneradorNominaXml.csproj: proyecto clásico de Windows Forms para .NET Framework 4.8.
- Program.cs: punto de entrada de la aplicación.
- Form1.cs: interfaz completa, selección de TXT/carpeta, progreso y bitácora.
- NominaXmlGenerator.cs: parser de registros pipe y generación de un XML por empleado.
- Modelos.cs: clases DTO del layout.

COMO ABRIR
1. Extraer el ZIP en una carpeta local.
2. Abrir GeneradorNominaXml.csproj con Visual Studio 2019 o 2022.
3. Verificar que esté instalado el targeting pack de .NET Framework 4.8.
4. Compilar en Debug o Release y ejecutar.
5. Seleccionar el TXT, carpeta destino y folio inicial; pulsar GENERAR XML.

FORMATO ESPERADO
Se conserva el layout delimitado por pipe y los registros EMP, CFD, NMM, E02, RPT, SNC, OTP y P01.
Los importes se interpretan con punto decimal (InvariantCulture), igual que el código original.

NOTAS IMPORTANTES
- El folio del primer XML ahora es exactamente el folio inicial indicado en pantalla; los siguientes se incrementan de uno en uno.
- La estructura y los cálculos principales se conservaron del código compartido. No se certifica que el XML sea válido para timbrado: antes de producción debe validarse contra XSD vigentes y reglas de negocio del SAT/Anexo 20 y complemento Nómina 1.2.
- El código original calcula SubTotal/Total como percepciones + otros pagos y no procesa deducciones. Se mantuvo esa lógica expresamente; confirmar el tratamiento fiscal antes de timbrar.
- El código original fija UsoCFDI=CN01 y RegimenFiscalReceptor=605, y genera SubsidioAlEmpleo para cada OtroPago. Deben confirmarse esos valores/condiciones con los datos reales y catálogos vigentes.
- Si el layout contiene otro orden de registros, el parser mantiene el empleado E02 actual hasta encontrar otro E02; RPT/SNC/OTP/P01 se asocian a ese empleado.

using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneradorNominaXml
{
    public class Form1 : Form
    {
        private TextBox txtArchivo;
        private TextBox txtSalida;
        private Button btnArchivo;
        private Button btnSalida;
        private Button btnGenerar;
        private Button btnAbrirCarpeta;
        private TextBox txtLog;
        private Label lblEstado;
        private Label lblResumen;
        private ProgressBar barraProgreso;
        private NumericUpDown nudFolio;
        private string ultimaCarpeta = "";

        public Form1()
        {
            CrearInterfaz();
        }

        private void CrearInterfaz()
        {
            Text = "Generador XML de Nómina CFDI 4.0";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 590);
            Size = new Size(900, 680);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(245, 247, 250);

            Label titulo = new Label { Text = "GENERADOR DE XML DE NÓMINA", Font = new Font("Segoe UI", 17F, FontStyle.Bold), ForeColor = Color.FromArgb(35, 55, 80), Location = new Point(25, 20), Size = new Size(700, 38) };
            Controls.Add(titulo);
            Label subtitulo = new Label { Text = "CFDI 4.0  |  Complemento de Nómina 1.2", ForeColor = Color.DimGray, Location = new Point(28, 58), Size = new Size(600, 24) };
            Controls.Add(subtitulo);

            Controls.Add(new Label { Text = "Archivo TXT de entrada", Location = new Point(28, 103), Size = new Size(220, 22), Font = new Font(Font, FontStyle.Bold) });
            txtArchivo = new TextBox { Location = new Point(28, 128), Size = new Size(700, 27), ReadOnly = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            Controls.Add(txtArchivo);
            btnArchivo = new Button { Text = "Examinar...", Location = new Point(740, 126), Size = new Size(115, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnArchivo.Click += BtnArchivo_Click; Controls.Add(btnArchivo);

            Controls.Add(new Label { Text = "Carpeta de destino", Location = new Point(28, 174), Size = new Size(220, 22), Font = new Font(Font, FontStyle.Bold) });
            txtSalida = new TextBox { Location = new Point(28, 199), Size = new Size(700, 27), ReadOnly = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            Controls.Add(txtSalida);
            btnSalida = new Button { Text = "Examinar...", Location = new Point(740, 197), Size = new Size(115, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSalida.Click += BtnSalida_Click; Controls.Add(btnSalida);

            Controls.Add(new Label { Text = "Folio inicial", Location = new Point(28, 244), Size = new Size(100, 22), Font = new Font(Font, FontStyle.Bold) });
            nudFolio = new NumericUpDown { Location = new Point(28, 269), Size = new Size(130, 27), Minimum = 0, Maximum = 999999999, Value = 1, ThousandsSeparator = false };
            Controls.Add(nudFolio);
            btnGenerar = new Button { Text = "GENERAR XML", Location = new Point(190, 263), Size = new Size(180, 39), BackColor = Color.FromArgb(35, 105, 165), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnGenerar.FlatAppearance.BorderSize = 0; btnGenerar.Click += BtnGenerar_Click; Controls.Add(btnGenerar);
            btnAbrirCarpeta = new Button { Text = "Abrir carpeta", Location = new Point(385, 263), Size = new Size(130, 39), Enabled = false };
            btnAbrirCarpeta.Click += BtnAbrirCarpeta_Click; Controls.Add(btnAbrirCarpeta);

            lblEstado = new Label { Text = "Estado: esperando archivo de entrada.", Location = new Point(28, 320), Size = new Size(820, 23), ForeColor = Color.FromArgb(65, 75, 90), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            Controls.Add(lblEstado);
            barraProgreso = new ProgressBar { Location = new Point(28, 348), Size = new Size(827, 20), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Minimum = 0, Maximum = 100 };
            Controls.Add(barraProgreso);
            lblResumen = new Label { Text = "Empleados procesados: 0", Location = new Point(28, 378), Size = new Size(820, 22), ForeColor = Color.DimGray };
            Controls.Add(lblResumen);
            Controls.Add(new Label { Text = "Bitácora del proceso", Location = new Point(28, 410), Size = new Size(250, 22), Font = new Font(Font, FontStyle.Bold) });
            txtLog = new TextBox { Location = new Point(28, 436), Size = new Size(827, 175), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            Controls.Add(txtLog);
        }

        private void BtnArchivo_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Archivos de texto (*.TXT;*.txt)|*.TXT;*.txt|Todos los archivos (*.*)|*.*";
                dialog.Title = "Seleccionar archivo de layout de nómina";
                if (dialog.ShowDialog(this) == DialogResult.OK) { txtArchivo.Text = dialog.FileName; AgregarLog("Archivo seleccionado: " + dialog.FileName); }
            }
        }

        private void BtnSalida_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Selecciona la carpeta donde se guardarán los XML";
                if (Directory.Exists(txtSalida.Text)) dialog.SelectedPath = txtSalida.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK) { txtSalida.Text = dialog.SelectedPath; AgregarLog("Carpeta destino: " + dialog.SelectedPath); }
            }
        }

        private async void BtnGenerar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtArchivo.Text) || !File.Exists(txtArchivo.Text))
            { MessageBox.Show(this, "Selecciona un archivo TXT válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtSalida.Text))
            { MessageBox.Show(this, "Selecciona la carpeta de destino.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            CambiarControles(false);
            barraProgreso.Value = 0;
            lblResumen.Text = "Empleados procesados: 0";
            txtLog.Clear();
            AgregarLog("Inicio: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            AgregarLog("Leyendo y validando el layout TXT...");
            string archivo = txtArchivo.Text;
            string salida = txtSalida.Text;
            int folio = Decimal.ToInt32(nudFolio.Value);
            var progress = new Progress<ProgresoGeneracion>(p =>
            {
                int porcentaje = p.Total == 0 ? 0 : (int)(p.Actual * 100.0 / p.Total);
                barraProgreso.Value = Math.Max(0, Math.Min(100, porcentaje));
                lblEstado.Text = string.Format("Procesando empleado {0} de {1}: {2}", p.Actual, p.Total, p.Empleado);
                lblResumen.Text = string.Format("Empleados procesados: {0} de {1}", p.Actual, p.Total);
                AgregarLog("[{0}/{1}] Empleado {2} - {3}".Replace("{0}", p.Actual.ToString()).Replace("{1}", p.Total.ToString()).Replace("{2}", p.Empleado).Replace("{3}", p.Archivo));
            });

            try
            {
                NominaXmlGenerator generator = new NominaXmlGenerator();
                int cantidad = await Task.Run(() => generator.GenerarArchivosXml(archivo, salida, folio, progress));
                barraProgreso.Value = 100;
                lblEstado.Text = "Proceso terminado correctamente.";
                lblResumen.Text = "XML generados: " + cantidad;
                ultimaCarpeta = salida;
                btnAbrirCarpeta.Enabled = true;
                AgregarLog("Proceso finalizado: " + cantidad + " archivo(s) XML generado(s).");
                AgregarLog("Carpeta de salida: " + salida);
                MessageBox.Show(this, "Proceso terminado correctamente.\r\n\r\nArchivos XML generados: " + cantidad + "\r\nCarpeta: " + salida,
                    "Generación concluida", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblEstado.Text = "Error durante la generación.";
                AgregarLog("ERROR: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Error al generar XML", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { CambiarControles(true); }
        }

        private void BtnAbrirCarpeta_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(ultimaCarpeta))
                System.Diagnostics.Process.Start("explorer.exe", ultimaCarpeta);
        }

        private void CambiarControles(bool habilitar)
        {
            btnGenerar.Enabled = habilitar; btnArchivo.Enabled = habilitar; btnSalida.Enabled = habilitar; nudFolio.Enabled = habilitar;
        }

        private void AgregarLog(string mensaje)
        {
            if (txtLog.InvokeRequired) { txtLog.BeginInvoke(new Action<string>(AgregarLog), mensaje); return; }
            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + mensaje + Environment.NewLine);
        }
    }
}

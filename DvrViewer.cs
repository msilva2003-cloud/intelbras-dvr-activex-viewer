// DVR Intelbras - navegador com motor do Internet Explorer (ActiveX)
// Compilar (32 bits, o plugin do DVR é 32 bits):
//   C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /target:winexe /platform:x86 /out:DvrViewer.exe DvrViewer.cs
// Uso: DvrViewer.exe [url]   (sem url: último endereço usado; na 1ª vez, http://192.168.1.108/, IP de fábrica Intelbras)

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DvrIntelbras
{
    static class Program
    {
        // 10001 = IE10 em modo padrão. A página do DVR só ativa o plugin quando o
        // user-agent contém "MSIE"; o IE11 (11001) não manda "MSIE" e cai no modo sem plugin.
        const int IeMode = 10001;
        const string EnderecoFabrica = "http://192.168.1.108/";

        static string ArquivoUltimoEndereco
        {
            get
            {
                return System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DvrViewer", "ultimo-endereco.txt");
            }
        }

        static string LerUltimoEndereco()
        {
            try
            {
                string u = System.IO.File.ReadAllText(ArquivoUltimoEndereco).Trim();
                if (u.Length > 0) return u;
            }
            catch { }
            return EnderecoFabrica;
        }

        public static void SalvarUltimoEndereco(string url)
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ArquivoUltimoEndereco));
                System.IO.File.WriteAllText(ArquivoUltimoEndereco, url);
            }
            catch { }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.ThreadException += (s, e) => MostrarErro(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => MostrarErro(e.ExceptionObject as Exception);
            try
            {
                string url = args.Length > 0 ? args[0] : LerUltimoEndereco();
                ConfigurarEmulacaoIE();
                Application.EnableVisualStyles();
                Application.Run(new JanelaPrincipal(url));
            }
            catch (Exception ex)
            {
                MostrarErro(ex);
            }
        }

        // Mostra o erro na tela e grava em DvrViewer-erro.txt, na Área de Trabalho.
        static void MostrarErro(Exception ex)
        {
            string texto = DateTime.Now + "\r\n" + ex;
            try
            {
                string arq = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "DvrViewer-erro.txt");
                System.IO.File.AppendAllText(arq, texto + "\r\n\r\n");
            }
            catch { }
            MessageBox.Show(texto, "DVR Intelbras - erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Grava, só para este .exe e só no usuário atual, qual versão do IE o controle emula.
        static void ConfigurarEmulacaoIE()
        {
            string exe = System.IO.Path.GetFileName(Application.ExecutablePath);
            const string chave = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\";
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(chave + "FEATURE_BROWSER_EMULATION"))
                k.SetValue(exe, IeMode, RegistryValueKind.DWord);
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(chave + "FEATURE_GPU_RENDERING"))
                k.SetValue(exe, 1, RegistryValueKind.DWord);
        }
    }

    class JanelaPrincipal : Form
    {
        readonly NavegadorDvr nav;
        readonly TextBox endereco;

        public JanelaPrincipal(string url)
        {
            Text = "DVR Intelbras";
            Width = 1400;
            Height = 900;
            StartPosition = FormStartPosition.CenterScreen;

            var barra = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            endereco = new TextBox { Text = url };
            var ir = new ToolStripButton("Ir");
            var recarregar = new ToolStripButton("Recarregar");
            var voltar = new ToolStripButton("Voltar");
            barra.Items.Add(voltar);
            barra.Items.Add(recarregar);
            barra.Items.Add(new ToolStripControlHost(endereco) { AutoSize = false, Width = 500 });
            barra.Items.Add(ir);
            endereco.Enter += (s, e) => BeginInvoke((Action)endereco.SelectAll);

            nav = new NavegadorDvr(new Uri(url).Host) { Dock = DockStyle.Fill };
            nav.ScriptErrorsSuppressed = true;
            nav.ErroNavegacao += (u, codigo) =>
                Text = string.Format("DVR Intelbras - erro 0x{0:X8} em {1}", codigo, u);
            nav.Navigated += (s, e) => endereco.Text = nav.Url.ToString();
            nav.DocumentTitleChanged += (s, e) => Text = "DVR Intelbras - " + nav.DocumentTitle;

            ir.Click += (s, e) => Navegar();
            recarregar.Click += (s, e) => nav.Refresh(WebBrowserRefreshOption.Completely);
            voltar.Click += (s, e) => nav.GoBack();
            endereco.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Navegar(); } };

            Controls.Add(nav);
            Controls.Add(barra);
            nav.Navigate(url);
        }

        void Navegar()
        {
            string u = endereco.Text.Trim();
            if (!u.StartsWith("http", StringComparison.OrdinalIgnoreCase)) u = "http://" + u;
            nav.HostLiberado = new Uri(u).Host;
            Program.SalvarUltimoEndereco(u);
            nav.Navigate(u);
        }
    }

    // WebBrowser com gerenciador de segurança próprio: libera ActiveX apenas para o
    // host do DVR, sem mexer nas zonas de segurança do Windows.
    class NavegadorDvr : WebBrowser
    {
        public string HostLiberado;

        public event Action<string, int> ErroNavegacao;
        AxHost.ConnectionPointCookie cookie;

        public NavegadorDvr(string host) { HostLiberado = host; }

        protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
        {
            return new SiteSeguro(this);
        }

        // O WebBrowser do WinForms não expõe NavigateError; liga o evento direto no controle COM.
        protected override void CreateSink()
        {
            base.CreateSink();
            cookie = new AxHost.ConnectionPointCookie(ActiveXInstance, new Eventos(this), typeof(DWebBrowserEvents2));
        }

        protected override void DetachSink()
        {
            if (cookie != null) { cookie.Disconnect(); cookie = null; }
            base.DetachSink();
        }

        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.None)]
        class Eventos : StandardOleMarshalObject, DWebBrowserEvents2
        {
            readonly NavegadorDvr dono;
            public Eventos(NavegadorDvr dono) { this.dono = dono; }

            public void NavigateError(object pDisp, ref object url, ref object frame, ref object statusCode, ref bool cancel)
            {
                if (dono.ErroNavegacao != null) dono.ErroNavegacao(Convert.ToString(url), Convert.ToInt32(statusCode));
            }
        }

        [ComVisible(true)]
        class SiteSeguro : WebBrowserSite, IServiceProvider, IInternetSecurityManager
        {
            static Guid IID_ISecurityManager = new Guid("79eac9ee-baf9-11ce-8c82-00aa004ba90b");
            const int S_OK = 0;
            const int E_NOINTERFACE = unchecked((int)0x80004002);
            const int INET_E_DEFAULT_ACTION = unchecked((int)0x800C0011);
            const int URLPOLICY_ALLOW = 0;

            readonly NavegadorDvr dono;

            public SiteSeguro(NavegadorDvr dono) : base(dono) { this.dono = dono; }

            public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
            {
                if (guidService == IID_ISecurityManager && riid == IID_ISecurityManager)
                {
                    ppvObject = Marshal.GetComInterfaceForObject(this, typeof(IInternetSecurityManager));
                    return S_OK;
                }
                ppvObject = IntPtr.Zero;
                return E_NOINTERFACE;
            }

            bool EhDoDvr(string url)
            {
                Uri u;
                return !string.IsNullOrEmpty(url)
                    && Uri.TryCreate(url, UriKind.Absolute, out u)
                    && string.Equals(u.Host, dono.HostLiberado, StringComparison.OrdinalIgnoreCase);
            }

            public int SetSecuritySite(IntPtr pSite) { return INET_E_DEFAULT_ACTION; }
            public int GetSecuritySite(out IntPtr pSite) { pSite = IntPtr.Zero; return INET_E_DEFAULT_ACTION; }
            public int MapUrlToZone(string url, out int zone, int flags) { zone = 0; return INET_E_DEFAULT_ACTION; }
            public int GetSecurityId(string url, IntPtr id, ref int size, int reserved) { return INET_E_DEFAULT_ACTION; }

            public int ProcessUrlAction(string url, int action, IntPtr pPolicy, int cbPolicy,
                IntPtr pContext, int cbContext, int flags, int reserved)
            {
                if (!EhDoDvr(url) || pPolicy == IntPtr.Zero || cbPolicy < 4) return INET_E_DEFAULT_ACTION;
                Marshal.WriteInt32(pPolicy, URLPOLICY_ALLOW);
                return S_OK;
            }

            public int QueryCustomPolicy(string url, ref Guid key, out IntPtr ppPolicy, out int pcbPolicy,
                IntPtr pContext, int cbContext, int reserved)
            {
                ppPolicy = IntPtr.Zero;
                pcbPolicy = 0;
                return INET_E_DEFAULT_ACTION;
            }

            public int SetZoneMapping(int zone, string pattern, int flags) { return INET_E_DEFAULT_ACTION; }
            public int GetZoneMappings(int zone, out IntPtr ppEnum, int flags) { ppEnum = IntPtr.Zero; return INET_E_DEFAULT_ACTION; }
        }
    }

    [ComImport, Guid("34A715A0-6587-11D0-924A-0020AFC7AC4D"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface DWebBrowserEvents2
    {
        [DispId(271)]
        void NavigateError([MarshalAs(UnmanagedType.IDispatch)] object pDisp, ref object url, ref object frame,
            ref object statusCode, ref bool cancel);
    }

    [ComImport, Guid("6d5140c1-7436-11ce-8034-00aa006009fa"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IServiceProvider
    {
        [PreserveSig]
        int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject);
    }

    [ComImport, Guid("79eac9ee-baf9-11ce-8c82-00aa004ba90b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IInternetSecurityManager
    {
        [PreserveSig] int SetSecuritySite(IntPtr pSite);
        [PreserveSig] int GetSecuritySite(out IntPtr pSite);
        [PreserveSig] int MapUrlToZone([MarshalAs(UnmanagedType.LPWStr)] string url, out int zone, int flags);
        [PreserveSig] int GetSecurityId([MarshalAs(UnmanagedType.LPWStr)] string url, IntPtr id, ref int size, int reserved);
        [PreserveSig] int ProcessUrlAction([MarshalAs(UnmanagedType.LPWStr)] string url, int action, IntPtr pPolicy, int cbPolicy,
            IntPtr pContext, int cbContext, int flags, int reserved);
        [PreserveSig] int QueryCustomPolicy([MarshalAs(UnmanagedType.LPWStr)] string url, ref Guid key, out IntPtr ppPolicy, out int pcbPolicy,
            IntPtr pContext, int cbContext, int reserved);
        [PreserveSig] int SetZoneMapping(int zone, [MarshalAs(UnmanagedType.LPWStr)] string pattern, int flags);
        [PreserveSig] int GetZoneMappings(int zone, out IntPtr ppEnum, int flags);
    }
}

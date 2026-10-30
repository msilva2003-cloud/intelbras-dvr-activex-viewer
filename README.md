# DVR Intelbras — Acesso Web com ActiveX no Windows 10/11 (sem Internet Explorer)

**Ferramenta gratuita para abrir a interface web ("Acesso Web") de DVRs Intelbras antigos que exigem o plugin ActiveX `webrec`, agora que o Internet Explorer foi removido do Windows.**

Se você digita o IP do seu DVR Intelbras no navegador, faz login e **nada acontece**, se **as câmeras não aparecem**, se o site pede um **plugin que não instala**, ou se o console mostra `ocx.SetParams is not a function`, este projeto é para você.

➡️ **[Baixar a versão mais recente (Releases)](../../releases/latest)**

> Projeto independente e não oficial. Não tem vínculo com a Intelbras ou com a Dahua. "Intelbras" é marca de seus respectivos donos e aparece aqui só para identificar os equipamentos compatíveis.

---

## O problema

Os DVRs Intelbras mais antigos (plataforma Dahua "Web3.0") exibem o vídeo pela página web usando o controle ActiveX `webrec.ocx`, distribuído pelo próprio aparelho no arquivo `webrec.cab`. Hoje:

- Chrome, Firefox e Edge não executam ActiveX;
- o **modo Internet Explorer do Edge** se identifica como IE11, e a página do DVR só ativa o plugin quando o navegador se identifica como **"MSIE"** (IE10 ou anterior). Resultado: o login trava;
- o plugin `webrec.cab` **não é assinado digitalmente**, então nem o IE consegue instalá-lo sozinho.

## A solução

| Arquivo | O que faz |
|---|---|
| `DvrViewer.exe` | Navegador mínimo com o motor do Internet Explorer em **modo IE10** (32 bits), que libera o ActiveX **somente para o endereço do DVR**, sem mexer nas configurações de segurança do Windows. |
| `instalar-plugin.bat` | Baixa o `webrec.cab` **do seu próprio DVR**, extrai e registra os controles ActiveX em `C:\Program Files\webrec\VD3100` (o mesmo que o `webrec.inf` do plugin faria). |
| `desinstalar-plugin.bat` | Remove o plugin. |

Nenhum arquivo da Intelbras é redistribuído aqui. O plugin vem sempre do seu equipamento.

## Como usar

1. Baixe o `.zip` em **[Releases](../../releases/latest)** e extraia numa pasta.
2. Clique com o botão direito em **`instalar-plugin.bat`** → **Executar como administrador**. Informe o IP do DVR (ex.: `192.168.1.108`). Devem aparecer linhas **OK**.
3. Abra o **`DvrViewer.exe`**, digite o endereço do DVR na barra (ex.: `http://192.168.1.108/`) e clique em **Ir**.
4. Faça login normalmente. As câmeras aparecem.

O app lembra o último endereço usado. Você também pode criar um atalho com o endereço como parâmetro: `DvrViewer.exe http://192.168.0.50/`.

Se o Windows mostrar *"O Windows protegeu o computador"*, clique em **Mais informações → Executar assim mesmo** (o executável não tem assinatura digital; o código-fonte completo está neste repositório).

## Compatibilidade

- Windows 10 e 11 (64 ou 32 bits), com .NET Framework 4 (já incluído no Windows).
- Testado num DVR Intelbras com interface "Acesso Web" Dahua3.0-Web3.0 que oferece `http://IP/webrec.cab`.
- Outros modelos Intelbras/Dahua com o mesmo plugin devem funcionar. Se o seu DVR usar outro arquivo de plugin, abra uma *issue* informando o modelo.

## Problemas comuns

- **"Não é possível acessar esta página"**: clique em **Ir** ou **Recarregar**; costuma ser passageiro. O título da janela mostra o código do erro.
- **O instalador não baixa o plugin**: confira o IP e se `http://IP-DO-DVR/` abre em qualquer navegador. Você também pode colocar o `webrec.cab` manualmente em `plugin\webrec.cab` ao lado do instalador.
- **Login entra mas as câmeras ficam pretas**: rode de novo o `instalar-plugin.bat` como administrador e reabra o app.
- **A janela não aparece**: com dois monitores, procure no segundo.
- **O app fecha sozinho**: ele grava `DvrViewer-erro.txt` na Área de Trabalho. Anexe esse arquivo numa *issue*.

## Segurança

- O motor do Internet Explorer é antigo. Use o DvrViewer **apenas para acessar o DVR na sua rede local**, não para navegar na internet.
- O ActiveX é liberado somente para o endereço (host) aberto na barra.
- O plugin da Intelbras não é assinado. Ele é baixado do seu próprio aparelho; instale só se confiar no equipamento.

## Compilar a partir do código

Não precisa de Visual Studio:

```bat
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /target:winexe /platform:x86 /out:DvrViewer.exe DvrViewer.cs
```

## Alternativas oficiais

- **Intelbras SIM Next** (software para PC) acessa o DVR pela porta 37777 sem plugin.
- **App Intelbras iSIC** no celular.

## Licença

[MIT](LICENSE). Contribuições são bem-vindas.

---

*Palavras-chave: DVR Intelbras não abre no navegador, Acesso Web Intelbras ActiveX, plugin webrec.cab, webrec.ocx, Intelbras Windows 11 sem Internet Explorer, câmeras não aparecem DVR Intelbras, ocx.SetParams is not a function, modo IE Edge DVR, Dahua web plugin, CFTV.*

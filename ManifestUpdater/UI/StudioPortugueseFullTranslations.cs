namespace ManifestUpdater;

internal static class StudioPortugueseFullTranslations
{
	private static readonly string[] Translations =
	[
		"Winget Manifest Studio", // 001
		"Ativar e testar instalação", // 002
		"Testes locais ativados", // 003
		"Opcional: testar no Sandbox", // 004
		"OS TESTES DE INSTALAÇÃO EXIGEM SUA CONFIRMAÇÃO", // 005
		"English", // 006
		"Español", // 007
		"Apelido", // 008
		"Direitos autorais", // 009
		"URL de direitos autorais", // 010
		"URL de compra", // 011
		"Canal", // 012
		"Tipo interno compartilhado", // 013
		"Conteúdo ZIP compartilhado", // 014
		"Protocolos", // 015
		"Extensões de arquivo", // 016
		"Arquiteturas não compatíveis", // 017
		"Códigos de sucesso adicionais", // 018
		"Nome da família do pacote", // 019
		"Comportamento de reparo", // 020
		"O instalador fecha o terminal", // 021
		"Local de instalação obrigatório", // 022
		"Exigir atualização explícita", // 023
		"Exibir avisos de instalação", // 024
		"Proibir comando de download", // 025
		"Binários do arquivo dependem do PATH", // 026
		"Parâmetro silencioso", // 027
		"Silencioso com progresso", // 028
		"Parâmetro interativo", // 029
		"Parâmetro do local de instalação", // 030
		"Parâmetro de log", // 031
		"Parâmetro de atualização", // 032
		"Parâmetro personalizado", // 033
		"Parâmetro de reparo", // 034
		"Termos", // 035
		"Links de documentação", // 036
		"Recursos restritos", // 037
		"Mercados permitidos", // 038
		"Mercados excluídos", // 039
		"Códigos de retorno esperados", // 040
		"Argumentos do Winget não compatíveis", // 041
		"Local de instalação padrão", // 042
		"Arquivos instalados", // 043
		"Tipo de autenticação", // 044
		"Recurso do Entra", // 045
		"Escopo do Entra", // 046
		"Campos de localidade adicionais", // 047
		"Campos de instalador adicionais", // 048
		"Desativado é mais seguro. Ative somente quando HTTPS não estiver disponível.", // 049
		"Acesso completo ao WingetCreate para New, Update, New-Locale, Update-Locale, Submit, Show, Token, Settings, Cache, Info e DSC. Os comandos são executados diretamente, sem cmd.exe. Comandos que fazem perguntas abrem um console real do WingetCreate para você responder.", // 050
		"SHA-256 DA ASSINATURA MSIX", // 051
		"Valores compartilhados por todos os arquivos de manifesto.", // 052
		"Informações exibidas aos usuários pelo Gerenciador de Pacotes do Windows.", // 053
		"Use links HTTPS públicos quando estiverem disponíveis.", // 054
		"Campos opcionais do esquema Winget atual. Deixe um campo vazio quando ele não se aplicar.", // 055
		"O Winget usa estes parâmetros de linha de comando para ações do instalador. Tipos conhecidos Inno, Nullsoft, MSI e MSIX geralmente não precisam de valores personalizados.", // 056
		"Formatos simples de uma linha criam o YAML aninhado para você. Use uma entrada por linha e deixe a caixa inteira vazia quando não se aplicar.", // 057
		"Regras opcionais para pacotes que dependem de outro pacote Winget ou recurso do Windows, recursos MSIX ou restrições de mercado.", // 058
		"Descreva resultados incomuns do instalador e arquivos instalados sem escrever YAML. Esses valores são opcionais e a validação oficial confere o esquema.", // 059
		"Somente fontes privadas protegidas pelo Entra ID usam estes campos. Pacotes do repositório comunitário devem deixar os três vazios.", // 060
		"Use estas caixas somente para campos do esquema que ainda não têm um controle guiado. Chaves personalizadas existentes continuam preservadas mesmo quando as caixas ficam vazias.", // 061
		"Formato obrigatório: Publisher.Application (exemplo: Contoso.Sample)", // 062
		"Não inclua v no início", // 063
		"Normalmente en-US", // 064
		"Escolha uma pasta vazia ou uma pasta de manifestos existente", // 065
		"Exemplo: MIT, Proprietary, Freeware", // 066
		"Separado por vírgulas", // 067
		"Aliases de comando separados por vírgulas. Preservados durante atualizações", // 068
		"Exibido ao usuário após a instalação", // 069
		"Versão do esquema usada pelo YAML gerado; 1.12.0 é recomendada para envios à comunidade Microsoft Winget", // 070
		"Nome público do produto que os usuários veem no Winget", // 071
		"Empresa ou pessoa que publica o aplicativo", // 072
		"Autor original do aplicativo quando diferente do publicador", // 073
		"Nome da licença, como MIT, GPL-3.0, Proprietary ou Freeware", // 074
		"Uma frase clara explicando o que o aplicativo faz", // 075
		"Uma explicação pública mais longa do aplicativo e de sua finalidade", // 076
		"Um apelido curto e fácil de digitar para localizar o pacote", // 077
		"Palavras de pesquisa separadas por vírgulas; não adicione #", // 078
		"Nomes de comandos instalados pelo pacote, separados por vírgulas", // 079
		"Página inicial HTTPS pública do publicador", // 080
		"Página HTTPS pública onde os usuários podem obter ajuda", // 081
		"Página HTTPS pública da política de privacidade", // 082
		"Página inicial HTTPS pública deste aplicativo", // 083
		"Página HTTPS pública com os termos da licença", // 084
		"Aviso de direitos autorais exibido com o pacote", // 085
		"Página HTTPS pública com informações de direitos autorais", // 086
		"Página de compra HTTPS pública quando o aplicativo é pago", // 087
		"Página HTTPS pública das notas desta versão exata", // 088
		"O que mudou nesta versão exata", // 089
		"Instruções que o Winget mostra após a instalação", // 090
		"Exemplo: stable ou beta", // 091
		"Exemplo: en-US", // 092
		"Separado por vírgulas; normalmente Windows.Desktop", // 093
		"Exemplo: 10.0.19041.0", // 094
		"Caminhos dentro do ZIP separados por ponto e vírgula; adicione | comando após um arquivo portátil quando necessário", // 095
		"Protocolos de URL separados por vírgulas", // 096
		"Separado por vírgulas, sem pontos", // 097
		"Números inteiros separados por vírgulas", // 098
		"AAAA-MM-DD", // 099
		"Tipo compartilhado opcional; linhas inspecionadas mantêm seu próprio tipo. Deixe vazio para instaladores mistos", // 100
		"Tipo real do instalador dentro de um pacote ZIP", // 101
		"Caminhos compartilhados dentro de um ZIP; separe por ponto e vírgula e adicione | comando somente para arquivos portáteis", // 102
		"Escopo compartilhado opcional; escolha user para uma conta, machine para todo o computador ou deixe vazio se variar por instalador", // 103
		"Modos compatíveis separados por vírgulas: interactive, silent, silentWithProgress", // 104
		"Instrução opcional para atualizações; deixe vazio a menos que o instalador exija um comportamento específico", // 105
		"Se o instalador exige elevação; deixe vazio quando não souber", // 106
		"Protocolos de URL registrados pelo aplicativo, separados por vírgulas", // 107
		"Extensões registradas pelo aplicativo, separadas por vírgulas e sem pontos", // 108
		"Arquiteturas que não podem usar este instalador, separadas por vírgulas", // 109
		"Códigos adicionais de saída bem-sucedida, separados por vírgulas", // 110
		"Nome da família de pacote Microsoft Store ou MSIX", // 111
		"Data pública da versão no formato AAAA-MM-DD", // 112
		"Como o Winget repara o aplicativo: modify, uninstaller ou installer", // 113
		"Digite true somente se a instalação fechar o terminal do usuário", // 114
		"Digite true somente quando um local de instalação personalizado for obrigatório", // 115
		"Digite true quando o Winget não puder atualizar automaticamente", // 116
		"Digite true quando o Winget deve mostrar avisos do instalador", // 117
		"Digite true quando winget download precisar ser bloqueado", // 118
		"Para arquivos compactados, digite true quando comandos extraídos dependerem do PATH", // 119
		"Argumento do instalador para uma instalação totalmente silenciosa", // 120
		"Argumento para instalação silenciosa com progresso", // 121
		"Argumento do instalador que força a interface interativa", // 122
		"Modelo de argumento para uma pasta de instalação personalizada", // 123
		"Modelo de argumento para o caminho do arquivo de log", // 124
		"Argumento usado especificamente durante atualizações", // 125
		"Argumento que o Winget deve adicionar a cada comando de instalação", // 126
		"Argumento do instalador usado para reparo", // 127
		"Um termo por linha no formato rótulo | URL HTTPS | texto do termo", // 128
		"Um link de documentação por linha no formato rótulo | URL HTTPS", // 129
		"Uma dependência Winget por linha no formato Publisher.Application | versão mínima", // 130
		"Nomes de recursos do Windows exigidos pelo aplicativo, separados por vírgulas", // 131
		"Recursos MSIX exigidos pelo pacote, separados por vírgulas", // 132
		"Recursos MSIX restritos, separados por vírgulas", // 133
		"Códigos de mercado onde a instalação é permitida, separados por vírgulas", // 134
		"Códigos de mercado onde a instalação é bloqueada, separados por vírgulas", // 135
		"Um resultado do instalador por linha no formato código | resposta Winget | URL HTTPS de ajuda opcional", // 136
		"Escolha log, location ou ambos somente quando o instalador não aceitar esses argumentos do Winget", // 137
		"Pasta normal do aplicativo instalado; variáveis de ambiente como %ProgramFiles% são permitidas", // 138
		"Um arquivo instalado por linha no formato caminho relativo | tipo de arquivo | SHA-256 opcional | argumento opcional | nome de exibição opcional", // 139
		"Autenticação para uma fonte privada; pacotes do repositório comunitário deixam isto vazio", // 140
		"Recurso Microsoft Entra usado por uma fonte privada", // 141
		"Escopo Microsoft Entra usado por uma fonte privada", // 142
		"Somente YAML avançado de localidade; a maioria dos usuários deve deixar vazio", // 143
		"Somente YAML avançado de instalador; a maioria dos usuários deve deixar vazio", // 144
		"Deixe vazio quando o valor não se aplicar ou for desconhecido", // 145
		"Pronto", // 146
		"Escolha o idioma usado pelo Studio. Dados do pacote e o YAML gerado nunca são traduzidos nem alterados.", // 147
		"A verificação de atualização precisa de atenção: {0}", // 148
		"Baixando e verificando a atualização selecionada...", // 149
		"Baixando do GitHub a atualização verificada do Studio...", // 150
		"Baixando... {0}%", // 151
		"Baixando e verificando {0}: {1}%", // 152
		"A atualização verificada está pronta. O Winget Manifest Studio será fechado para concluir a atualização.", // 153
		"O download da atualização foi cancelado. Nenhum arquivo do aplicativo foi alterado.", // 154
		"Crie um envio para o Winget sem editar YAML manualmente.", // 155
		"Crie um novo conjunto de três manifestos ou atualize um existente com segurança. Os arquivos locais fornecem o SHA-256 real; as URLs públicas informam ao Winget onde baixá-los.", // 156
		"LOCAL PRIMEIRO\n\nO token do GitHub fica no Gerenciador de Credenciais do Windows\nNenhum manifesto é sobrescrito sem backup\nNenhum instalador é baixado sem confirmação", // 157
		"Crie um pacote vazio, carregue arquivos YAML que já estão neste computador ou informe o ID de um pacote Winget existente para baixar os manifestos atuais em uma nova cópia de trabalho.", // 158
		"Carregar manifestos existentes", // 159
		"Importar pacote Winget existente", // 160
		"Criar um novo projeto", // 161
		"Digite os detalhes do pacote ou cole uma URL pública de versão do GitHub. O importador preenche somente campos vazios e pergunta antes de baixar arquivos compatíveis para calcular hashes e inspecionar o instalador.", // 162
		"Importar versão do GitHub", // 163
		"Abrir detalhes do pacote", // 164
		"Escolha os arquivos locais MSI, EXE, MSIX, APPX, ZIP, aplicativo portátil ou fonte que serão enviados. O Studio lê exatamente esses arquivos e calcula seus SHA-256. Depois, informe a URL pública de download de cada arquivo.", // 165
		"Abrir Instaladores e hashes", // 166
		"A visualização cria os três manifestos na memória. Salvar só os grava depois da validação e mantém backups com data e hora dos arquivos existentes.", // 167
		"Abrir Revisar e enviar", // 168
		"Abrir ferramentas oficiais", // 169
		"Abra o guia integrado para iniciantes sobre significado dos campos, IDs de instalador, hashes, validação e envio.", // 170
		"Mantenha o Winget Manifest Studio atualizado", // 171
		"A página inicial verifica a versão estável mais recente do GitHub depois que a janela já está aberta. Uma cópia instalada usa StudioSetup.msi; uma cópia portátil substitui somente WingetManifestStudio.exe. Nada é baixado nem instalado até você escolher o botão de atualização e confirmar.", // 172
		"Todas as caixas abaixo podem ser editadas. Carregar uma pasta lê apenas os arquivos YAML; nunca baixa instaladores nem altera os manifestos.", // 173
		"Campos avançados opcionais do pacote", // 174
		"A maioria dos iniciantes não precisa substituir o comportamento do instalador, usar parâmetros personalizados ou YAML avançado bruto. Abra esta seção somente quando a documentação do instalador ou um manifesto existente exigir um desses valores.", // 175
		"1 Adicione cada arquivo exato da versão. 2 Cole sua URL HTTPS pública direta. 3 Inspecione para preencher o hash e os metadados. 4 Verifique as URLs depois do envio. Arquitetura, tipo e escopo ficam visíveis ao lado da URL e podem ser corrigidos nas listas.", // 176
		"1 Adicionar arquivos da versão", // 177
		"2 Informar URL pública", // 178
		"3 Inspecionar e preencher seleção", // 179
		"4 Verificar URLs públicas", // 180
		"REVISAR E SALVAR COM SEGURANÇA", // 181
		"Use a única ação destacada abaixo. A revisão não altera arquivos até você escolher Salvar, e manifestos existentes são copiados antes da substituição.", // 182
		"LISTA DE REVISÃO", // 183
		"O Studio libera as etapas na ordem correta.", // 184
		"1  Visualizar", // 185
		"Cria o YAML proposto na memória", // 186
		"2  Salvar com segurança", // 187
		"Cria backups antes de substituir arquivos", // 188
		"3  Validar", // 189
		"Executa o validador oficial do Winget", // 190
		"4  Testar e enviar", // 191
		"Continua na Central de testes guiada", // 192
		"OPÇÕES DE EXIBIÇÃO\r\nA revisão em linguagem simples fica selecionada por padrão.", // 193
		"Mostrar YAML técnico", // 194
		"Mostrar revisão em linguagem simples", // 195
		"Abrir pasta de backup", // 196
		"REVISÃO EM LINGUAGEM SIMPLES", // 197
		"Corrigir as informações do pacote", // 198
		"O Studio levará você de volta à página correta.", // 199
		"OBRIGATÓRIO · A visualização fica bloqueada até a correção", // 200
		"OBRIGATÓRIO · Os testes ficam bloqueados até a correção", // 201
		"A versão do pacote é obrigatória e não pode começar com v", // 202
		"O nome do pacote é obrigatório", // 203
		"O publicador é obrigatório", // 204
		"A descrição curta é obrigatória", // 205
		"A licença é obrigatória", // 206
		"Escolha uma pasta de saída dos manifestos", // 207
		"Adicione pelo menos um instalador", // 208
		"Abrir o campo para corrigir", // 209
		"Corrigir o problema de validação", // 210
		"O resultado em linguagem simples abaixo informa o problema e onde corrigi-lo. Depois visualize e salve novamente.", // 211
		"PARAR · O envio fica bloqueado até a validação passar", // 212
		"Abrir os campos para corrigir", // 213
		"Visualizar as alterações propostas", // 214
		"Cria na memória as alterações exatas dos manifestos e as explica abaixo. Nenhum arquivo é gravado.", // 215
		"SEGURO · A visualização não altera arquivos", // 216
		"Salvar os manifestos revisados", // 217
		"Grava o YAML revisado na pasta de saída depois de criar backups recuperáveis dos arquivos existentes.", // 218
		"PROTEGIDO · Manifestos existentes são copiados primeiro", // 219
		"Validar com o Winget", // 220
		"Executa o validador Winget da Microsoft em uma cópia temporária limpa. O pacote não é instalado.", // 221
		"SEGURO · A validação não altera os manifestos salvos", // 222
		"Continuar para a Central de testes", // 223
		"Execute a pré-verificação segura, teste a instalação, verifique o resultado e envie em uma única tela guiada.", // 224
		"PRÓXIMO · Testes e envio continuam sem voltar aqui", // 225
		"Pronto para enviar na Central de testes", // 226
		"Todas as revisões e testes de instalação obrigatórios passaram. A ação de envio está pronta na Central de testes.", // 227
		"PRONTO · O WingetCreate da Microsoft cuida do envio", // 228
		"O WINGET ENCONTROU UM PROBLEMA   •   NADA FOI ENVIADO", // 229
		"VISUALIZAÇÃO PRONTA   •   NADA FOI SALVO", // 230
		"SALVO COM SEGURANÇA   •   PRONTO PARA A VALIDAÇÃO OFICIAL", // 231
		"VALIDAÇÃO APROVADA   •   PRONTO PARA A CENTRAL DE TESTES", // 232
		"TODAS AS REVISÕES E TESTES DE INSTALAÇÃO PASSARAM", // 233
		"Abrir a Central de testes para enviar", // 234
		"TESTAR E CONCLUIR", // 235
		"Siga a linha de progresso e use a única ação destacada abaixo. O Studio libera cada teste na ordem correta e habilita o envio quando todos os quatro passam.", // 236
		"LISTA OBRIGATÓRIA", // 237
		"Estas etapas são concluídas automaticamente na ordem.", // 238
		"1  Pré-verificação segura", // 239
		"Verificações de manifesto, hash, assinatura e repositório", // 240
		"2  Testes locais", // 241
		"Configuração única do Windows", // 242
		"3  Teste de instalação", // 243
		"Instala esta versão exata pelo Winget", // 244
		"4  Resultado instalado", // 245
		"Confirma a versão instalada", // 246
		"DIAGNÓSTICOS OPCIONAIS\r\nSomente detalhes extras — não são etapas obrigatórias.", // 247
		"Verificar configuração do Winget", // 248
		"Somente instalar no Sandbox", // 249
		"Instalar e desinstalar no Sandbox", // 250
		"RESULTADOS E INSTRUÇÕES", // 251
		"Reparar a configuração de teste do Winget", // 252
		"O Gerenciador de Pacotes do Windows não está pronto. Execute a verificação de configuração para ver as instruções exatas de reparo.", // 253
		"SEGURO · Apenas verifica o Winget e não altera nada", // 254
		"Verifica YAML, hashes, assinaturas, validação oficial do Winget e se este pacote já existe.", // 255
		"SEGURO · Nada será instalado ou alterado", // 256
		"Permitir testes de manifestos locais", // 257
		"O Windows exige uma aprovação de administrador uma única vez antes que o Winget possa instalar um manifesto deste computador.", // 258
		"CONFIGURAÇÃO ÚNICA · Aprove a solicitação do Windows", // 259
		"Testar a instalação desta versão", // 260
		"Executa winget install --manifest com os arquivos exatos gerados. Revise o console do instalador e depois feche-o.", // 261
		"CONFIRMAÇÃO OBRIGATÓRIA · Isto instala software neste PC", // 262
		"Confirmar o resultado instalado", // 263
		"Verifica o ID do pacote Winget e, quando necessário, a identidade MSI ou o nome do aplicativo instalado.", // 264
		"SEGURO · A verificação não reinstala o pacote", // 265
		"Verificar instalação", // 266
		"Todos os testes passaram — pronto para enviar", // 267
		"Inicie o envio oficial do WingetCreate da Microsoft sem voltar à página de revisão.", // 268
		"PRONTO · O WingetCreate cuida do login e da criação da solicitação de pull", // 269
		"Pasta de backup", // 270
		"Ainda não há backups", // 271
		"Teste de instalação e desinstalação no Sandbox", // 272
		"Este guia explica cada tela e as informações exigidas pelo Winget. Você pode lê-lo a qualquer momento; os botões apenas levam à tela descrita.", // 273
		"Iniciar ou abrir um projeto de manifesto", // 274
		"Para uma primeira versão, escolha Novo projeto. Para uma atualização, carregue uma pasta YAML local ou escolha Importar pacote Winget existente e informe o ID exato. A importação do repositório baixa os manifestos mais recentes em uma pasta de trabalho separada e nunca sobrescreve uma pasta existente.", // 275
		"Ir para Detalhes do pacote", // 276
		"Informar a identidade do pacote", // 277
		"O identificador do pacote é o nome permanente no Winget, normalmente Publisher.Application. Informe primeiro Publicador e Nome do pacote e, se quiser, use Sugerir ID. A versão não tem v no início. Mantenha o identificador igual nas atualizações.", // 278
		"Editar identidade do pacote", // 279
		"Completar as informações públicas do pacote", // 280
		"Nome do pacote, Publicador, Licença e Descrição curta são obrigatórios. Digite-os ou use Importar versão do GitHub na página inicial. O importador preenche somente campos vazios e pergunta antes de baixar temporariamente arquivos compatíveis. Campos guiados opcionais criam dependências, termos, documentação, códigos de retorno, regras de mercado e detecção de instalação sem editar YAML manualmente.", // 281
		"Editar informações do pacote", // 282
		"ARQUIVOS DO INSTALADOR E LINKS DE DOWNLOAD", // 283
		"O Winget baixa de uma URL pública, mas o Studio usa o arquivo local correspondente para calcular o SHA-256 confiável.", // 284
		"Adicionar o arquivo exato da versão", // 285
		"Escolha Adicionar arquivos da versão para cada instalador publicado. Selecione o mesmo MSI, EXE, MSIX, APPX, pacote, ZIP, aplicativo portátil ou fonte que será enviado. Use uma linha para cada arquitetura, escopo ou variação. Nada é presumido como x64.", // 286
		"Informar sua URL HTTPS pública", // 287
		"Cole a URL direta de download de cada instalador, não uma página com botão de download. A URL deve permanecer pública e baixar exatamente o arquivo local daquela linha. URLs de arquivos de versões do GitHub são adequadas.", // 288
		"Informar URLs de download", // 289
		"Inspecionar e verificar o instalador publicado", // 290
		"Inspecionar e preencher calcula o SHA-256, informa se o arquivo é assinado ou não e detecta sinais de MSI, MSIX, Inno, NSIS, WiX Burn, Squirrel, Velopack, InstallShield, Advanced Installer e EXE autoextraível. EXE/MSI não assinados são aceitos e exibidos como aviso; MSIX/APPX ainda exigem assinatura do pacote. ZIPs mostram caminhos internos. Verificar URLs públicas comprova que o arquivo publicado corresponde ao hash.", // 291
		"Inspecionar arquivos do instalador", // 292
		"TIPOS ESPECIAIS DE PACOTE", // 293
		"EXEs portáteis podem parecer instaladores EXE normais; escolha portable na linha quando necessário. Pacotes de fontes usam a raiz separada fonts da Microsoft e têm regras mais rígidas. O suporte a PWA varia conforme o cliente Winget e a política do repositório; sempre confira a validação oficial e o teste de instalação.", // 294
		"REVISAR, SALVAR E PUBLICAR", // 295
		"A visualização é sua verificação de segurança. Ela cria o YAML proposto na memória sem gravar na pasta selecionada.", // 296
		"Seguir a prontidão do projeto e visualizar", // 297
		"O painel de prontidão conta os dados obrigatórios pendentes e marca campos com problemas. Quando mostrar PRONTO, escolha Visualizar alterações e confira identificador, versões antiga e nova, URLs, arquiteturas, tipos de instalador, hashes e nomes de arquivo.", // 298
		"Revisar a visualização", // 299
		"Salvar com backups recuperáveis", // 300
		"Escolha Salvar manifestos somente quando a visualização estiver correta. Novos arquivos são criados na pasta de saída. Arquivos existentes são copiados para uma pasta .manifest-backups com data e hora antes da substituição.", // 301
		"Salvar ou validar", // 302
		"Validar antes do envio", // 303
		"Validar localmente executa o validador oficial do Winget em uma cópia temporária limpa. Se houver erro, corrija o campo relacionado e valide novamente. A validação não altera os manifestos salvos.", // 304
		"Abrir validação", // 305
		"Executar etapa 1 — Pré-verificação segura", // 306
		"A Central de testes primeiro verifica o próprio Winget, depois confere novamente hashes e assinaturas dos arquivos anexados, executa a validação oficial e procura o identificador exato no Winget e em microsoft/winget-pkgs. Nada é instalado.", // 307
		"Executar etapas 2, 3 e 4", // 308
		"Ativar testes locais pede uma aprovação de administrador do Windows uma única vez. Testar instalação aqui valida novamente antes de winget install --manifest. Verificar instalação confere o ID do Winget e usa o ProductCode MSI exato ou o nome do aplicativo instalado quando o Winget não mantém o ID local.", // 309
		"Abrir testes de instalação", // 310
		"Usar o Windows Sandbox quando disponível", // 311
		"A instalação no Sandbox executa o SandboxTest.ps1 oficial da Microsoft em um ambiente descartável. Instalar e desinstalar no Sandbox também verifica a remoção antes de fechar. A primeira execução pode levar vários minutos para preparar dependências da Microsoft. Um manifesto com elevationProhibited deve usar Testar instalação aqui, porque o Sandbox executa o Winget como administrador.", // 312
		"Abrir teste no Sandbox", // 313
		"Enviar diretamente da Central de testes", // 314
		"Depois que os quatro testes obrigatórios passarem, escolha Enviar para o Winget na parte inferior das etapas da Central. O fluxo oficial do WingetCreate da Microsoft abre para login e criação da solicitação de pull. O token do GitHub fica no Gerenciador de Credenciais do Windows.", // 315
		"Não use v no início da versão, uma URL de página da versão em vez da URL direta do arquivo, um hash de outro arquivo ou a arquitetura errada. Para pacotes ZIP, revise TIPO INTERNO e CONTEÚDO ZIP. Anexe novamente e inspecione o arquivo publicado exato sempre que ele mudar.", // 316
	];

	public static readonly IReadOnlyDictionary<string, string> Values =
		StudioFullTranslationCatalog.Create(Translations, "pt-BR");
}

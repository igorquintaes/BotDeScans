# Bot de Scans

Um bot de Discord com diversas ferramentas para facilitar o dia a dia e o trabalho de grupos Scanlators, idealizado e desenvolvido pela [Escola de Scans](https://www.youtube.com/c/EscoladeScans)! Ele é totalmente gratuito, fique à vontade para contribuir se desejar. Aprenda já a usá-lo em nossa [Wiki](https://github.com/igorquintaes/BotDeScans/wiki). Lá ensinamos como baixar e configurar. Não é preciso ter tanto conhecimento com computadores nem com programação. 

## Como funciona

Seu principal objetivo, agora em suas versões iniciais, é providenciar uma melhor qualidade de vida e facilidade ao lidar com publicações de novos lançamentos. Sabe aquele trabalho chato e cansativo de publicar e divulgar os lançamentos em inúmeros lugares? A ideia é acabar com a burocracia e transformar tudo em uma única execução através do Discord! 

![image](https://github.com/user-attachments/assets/cf99049e-8cea-45ae-a114-75ae634c721a)

Ele também realiza validações em arquivos prontos para serem publicados, garantindo que o trabalho final conténham as páginas ordenadas, página de créditos e muito mais! Em caso de qualquer erro de validação, ou erros gerais, um log detalhado é exibido via mensagem do Discord, na janela do programa ou em um arquivo de log na mesma pasta do programa.

![image](https://github.com/user-attachments/assets/a494a482-942b-40f9-920c-f5ddad6fe822)

## Execução de testes e cobertura

Os testes usam Microsoft.Testing.Platform (MTP). Com o .NET 10 SDK e o PowerShell 7 (`pwsh`) instalados, execute o script multiplataforma na raiz do repositório:

```powershell
pwsh -NoLogo -NoProfile -File ./reportgenerator.ps1
```

O comando compila a solução em `Release`, executa os testes unitários e de integração com cobertura nativa do MTP e gera os arquivos Cobertura em `coverage/` e o relatório HTML em `TestResults/Report/index.html`. O resumo também é exibido no terminal. Para pular a compilação quando os binários já estiverem atualizados, execute `pwsh -NoLogo -NoProfile -File ./reportgenerator.ps1 -NoBuild`.

O mesmo script é usado pela pipeline do GitHub Actions; a etapa de build da pipeline compila a solução antes e chama o script com `-NoBuild`.

Para mais conteúdos de scanaltors, acesse: [Escola de Scans](https://github.com/igorquintaes/BotDeScans/wiki).

// COMMON OS ALIASES
alias("-install", "install.js")
alias("-uninstall", "uninstall.js")
alias("exit", "exitcmd.js")


// NETWORK ALIASES
alias("connect", "connect.js")
alias("send", "sendfile.js")
alias("get", "sendfile.js")
alias('upload', 'upload.js');


install("example.app")
install("graphicTest.app")
install("transfer.app")
install("test.web")
install("webapp.web")
install("networkInterface.app")

/*
call('config set DEFAULT_SERVER_IP 192.168.0.141')
call('config set ALWAYS_CONNECT true')
*/
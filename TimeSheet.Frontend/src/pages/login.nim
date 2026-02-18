import karax / [karax, karaxdsl, vdom, kdom]
import ../state
import ../api

proc renderLogin*(): VNode =
  buildHtml(main(class = "container")):
    tdiv(class = "login-wrap"):
      article:
        header: h2: text "TimeSheet"
        p: text "Enter the one-time mnemonic from Telegram to log in."
        label(`for` = "mnemonic"): text "Mnemonic phrase"
        input(id = "mnemonic", `type` = "text",
              placeholder = "word1 word2 word3 …",
              autocomplete = "off"):
          proc oninput(ev: Event; n: VNode) =
            mnemonicVal = n.value
        if errorMsg != "":
          p(class = "error"): text errorMsg
        if loading:
          button(`aria-busy` = "true", disabled = "true"): text "Logging in…"
        else:
          button:
            proc onclick(ev: Event; n: VNode) =
              if mnemonicVal != "":
                doLogin(mnemonicVal)
            text "Login"

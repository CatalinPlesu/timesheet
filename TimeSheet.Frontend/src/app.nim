## Entry point — wires pages together and starts karax.

import karax / [karax, karaxdsl, vdom]
import state
import api
import components / nav
import pages / [login, tracking, entries, analytics]

proc createDom(): VNode =
  buildHtml(tdiv):
    renderNav()
    case page
    of pgLogin:     renderLogin()
    of pgTracking:  renderTracking()
    of pgEntries:   renderEntries()
    of pgAnalytics: renderAnalytics()

setRenderer createDom

# Restore session from localStorage
let stored = lsGet("ts_token")
if stored != "":
  token = stored
  page = pgTracking
  fetchCurrentState()

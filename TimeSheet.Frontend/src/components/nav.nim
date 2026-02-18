import karax / [karax, karaxdsl, vdom, kdom]
import ../state
import ../api

proc renderNav*(): VNode =
  buildHtml(tdiv):
    if page != pgLogin:
      nav(class = "container-fluid"):
        ul:
          li: strong: text "TimeSheet"
        ul:
          li:
            a(href = "#",
              class = if page == pgTracking: "contrast" else: ""):
              proc onclick(ev: Event; n: VNode) =
                page = pgTracking
                successMsg = cstring ""
                fetchCurrentState()
              text "Tracking"
          li:
            a(href = "#",
              class = if page == pgEntries: "contrast" else: ""):
              proc onclick(ev: Event; n: VNode) =
                page = pgEntries
                successMsg = cstring ""
                fetchEntries()
              text "Entries"
          li:
            a(href = "#",
              class = if page == pgAnalytics: "contrast" else: ""):
              proc onclick(ev: Event; n: VNode) =
                page = pgAnalytics
                successMsg = cstring ""
                fetchAllAnalytics()
              text "Analytics"
          li:
            a(href = "#"):
              proc onclick(ev: Event; n: VNode) =
                clearToken()
                page = pgLogin
              text "Logout"

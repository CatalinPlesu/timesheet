import karax / [karaxdsl, vdom]
import std / jsffi
import ../state

proc avgRow(label: cstring; hours: float): VNode =
  buildHtml(tr):
    td: text label
    td: text fmtDur(hours)

proc renderAnalytics*(): VNode =
  # Extract values before buildHtml to avoid cstring coercion issues inside macro
  var avgWork, avgCtoW, avgCtoH, avgLunch, avgTotal: float
  var daysIncluded, totalWorkDays: int
  if not anaData.isNull:
    avgWork      = anaData[cstring "averageWorkHours"].toFloat
    avgCtoW      = anaData[cstring "averageCommuteToWorkHours"].toFloat
    avgCtoH      = anaData[cstring "averageCommuteToHomeHours"].toFloat
    avgLunch     = anaData[cstring "averageLunchHours"].toFloat
    avgTotal     = anaData[cstring "averageTotalDurationHours"].toFloat
    daysIncluded  = anaData[cstring "daysIncluded"].toInt
    totalWorkDays = anaData[cstring "totalWorkDays"].toInt

  buildHtml(main(class = "container")):
    h2: text "Analytics"
    if errorMsg != "":
      p(class = "error"): text errorMsg
    if loading:
      p(`aria-busy` = "true"): text "Loading…"
    elif anaData.isNull:
      p: text "No data yet."
    else:
      article:
        header: text "Daily averages"
        table:
          thead:
            tr:
              th: text "Metric"
              th: text "Average"
          tbody:
            avgRow("Work",           avgWork)
            avgRow("Commute → work", avgCtoW)
            avgRow("Commute → home", avgCtoH)
            avgRow("Lunch",          avgLunch)
            avgRow("Total day",      avgTotal)
        p(class = "muted-sm"):
          text cstring("Based on " & $daysIncluded &
                       " days (" & $totalWorkDays & " work days)")

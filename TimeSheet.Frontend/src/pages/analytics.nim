import karax / [karax, karaxdsl, vdom, kdom]
import std / jsffi
import ../state
import ../api

# ─── JS chart interop ────────────────────────────────────────────────────────

proc renderChartFromJson*(id, labels, work, commute, lunch, idle: cstring) {.
  importjs: "renderChartFromJson(#,#,#,#,#,#)".}

proc chartExists*(id: cstring): bool {.
  importjs: "(document.getElementById(#) != null)".}

# ─── Chart render flag (avoid re-creating on every karax redraw) ─────────────

var chartDrawn = false

# ─── Stats row helper ────────────────────────────────────────────────────────

proc statsRow(label: cstring; obj: JsObject): VNode =
  var avg, mn, mx, sd, tot: float
  if not obj.isNull:
    avg = obj[cstring "avg"].toFloat
    mn  = obj[cstring "min"].toFloat
    mx  = obj[cstring "max"].toFloat
    sd  = obj[cstring "stdDev"].toFloat
    tot = obj[cstring "total"].toFloat
  let avgS = fmtDur(avg)
  let mnS  = fmtDur(mn)
  let mxS  = fmtDur(mx)
  let sdS  = fmtDur(sd)
  let totS = fmtDur(tot)
  buildHtml(tr):
    td: text label
    td: text avgS
    td: text mnS
    td: text mxS
    td: text sdS
    td: text totS

# ─── Main render ─────────────────────────────────────────────────────────────

proc renderAnalytics*(): VNode =
  # Extract stats values before buildHtml
  var periodDays, daysWithData: int
  var workObj, commuteObj, ctowObj, ctohObj, lunchObj: JsObject

  if not anaStats.isNull:
    periodDays   = anaStats[cstring "periodDays"].toInt
    daysWithData = anaStats[cstring "daysWithData"].toInt
    workObj      = anaStats[cstring "work"]
    commuteObj   = anaStats[cstring "commute"]
    ctowObj      = anaStats[cstring "commuteToWork"]
    ctohObj      = anaStats[cstring "commuteToHome"]
    lunchObj     = anaStats[cstring "lunch"]

  # Build chart data strings (for the chart tab)
  var labelsJson  = cstring "[]"
  var workJson    = cstring "[]"
  var commuteJson = cstring "[]"
  var lunchJson   = cstring "[]"
  var idleJson    = cstring "[]"

  if not anaChart.isNull:
    let lblArr = anaChart[cstring "labels"]
    let wArr   = anaChart[cstring "workHours"]
    let cArr   = anaChart[cstring "commuteHours"]
    let lArr   = anaChart[cstring "lunchHours"]
    let iArr   = anaChart[cstring "idleHours"]
    if not lblArr.isNull:
      labelsJson  = jsStringify(lblArr)
      workJson    = jsStringify(wArr)
      commuteJson = jsStringify(cArr)
      lunchJson   = jsStringify(lArr)
      idleJson    = jsStringify(iArr)

  # Build calendar data (extracted before buildHtml)
  type CalDay = tuple[label: cstring; wH, cH, lH: float; active: bool]
  var calDays: seq[CalDay]
  var maxDayHours = 0.001  # avoid division by zero
  if not anaBreakdown.isNull:
    let n = jsLen(anaBreakdown)
    for i in 0 ..< n:
      let d = anaBreakdown[i]
      let active = d[cstring "hasActivity"].toBool
      let w  = d[cstring "workHours"].toFloat
      let cw = d[cstring "commuteToWorkHours"].toFloat
      let ch = d[cstring "commuteToHomeHours"].toFloat
      let l  = d[cstring "lunchHours"].toFloat
      let c  = cw + ch
      # Trim date to "MM-DD" for compact display
      var raw = $d[cstring "date"].toStr
      var label: cstring
      if raw.len >= 10: label = cstring(raw[5..9])
      else:             label = cstring(raw)
      let total = w + c + l
      if total > maxDayHours: maxDayHours = total
      calDays.add((label, w, c, l, active))

  # Reset chartDrawn when not on chart tab (so it re-renders next time)
  let isChartTab = anaTab == cstring "chart"
  if not isChartTab:
    chartDrawn = false

  let pInfo = cstring($periodDays & " days in period, " &
                      $daysWithData & " days with data")

  # Capture chart json for post-render call (can't call void JS inside buildHtml)
  let capLabels  = labelsJson
  let capWork    = workJson
  let capCommute = commuteJson
  let capLunch   = lunchJson
  let capIdle    = idleJson

  result = buildHtml(main(class = "container")):
    h2: text "Analytics"

    if errorMsg != "":
      p(class = "error"): text errorMsg

    # ── Period selector ──────────────────────────────────────────────────────
    tdiv(class = "period-tabs"):
      button(class = if anaPeriod == 7:   "btn-compact btn-active" else: "btn-compact"):
        proc onclick(ev: Event; n: VNode) =
          anaPeriod = 7
          chartDrawn = false
          fetchAllAnalytics()
        text "7d"
      button(class = if anaPeriod == 30:  "btn-compact btn-active" else: "btn-compact"):
        proc onclick(ev: Event; n: VNode) =
          anaPeriod = 30
          chartDrawn = false
          fetchAllAnalytics()
        text "30d"
      button(class = if anaPeriod == 90:  "btn-compact btn-active" else: "btn-compact"):
        proc onclick(ev: Event; n: VNode) =
          anaPeriod = 90
          chartDrawn = false
          fetchAllAnalytics()
        text "90d"
      button(class = if anaPeriod == 365: "btn-compact btn-active" else: "btn-compact"):
        proc onclick(ev: Event; n: VNode) =
          anaPeriod = 365
          chartDrawn = false
          fetchAllAnalytics()
        text "365d"

    # ── Analytics sub-tabs ───────────────────────────────────────────────────
    tdiv(class = "ana-tabs"):
      button(class = if anaTab == cstring "stats":    "ana-tab active" else: "ana-tab"):
        proc onclick(ev: Event; n: VNode) =
          anaTab = cstring "stats"
          chartDrawn = false
        text "Stats"
      button(class = if anaTab == cstring "chart":    "ana-tab active" else: "ana-tab"):
        proc onclick(ev: Event; n: VNode) =
          anaTab = cstring "chart"
          chartDrawn = false
        text "Chart"
      button(class = if anaTab == cstring "calendar": "ana-tab active" else: "ana-tab"):
        proc onclick(ev: Event; n: VNode) =
          anaTab = cstring "calendar"
          chartDrawn = false
        text "Calendar"

    if loading:
      p(`aria-busy` = "true"): text "Loading…"

    # ── Stats tab ────────────────────────────────────────────────────────────
    elif anaTab == cstring "stats":
      if anaStats.isNull:
        p: text "No stats yet."
      else:
        p(class = "muted-sm"): text pInfo
        article:
          table(class = "stats-table"):
            thead:
              tr:
                th: text "Metric"
                th: text "Avg"
                th: text "Min"
                th: text "Max"
                th: text "Std Dev"
                th: text "Total"
            tbody:
              statsRow(cstring "Work",           workObj)
              statsRow(cstring "Commute ->Work", ctowObj)
              statsRow(cstring "Commute ->Home", ctohObj)
              statsRow(cstring "Lunch",          lunchObj)

    # ── Chart tab ────────────────────────────────────────────────────────────
    elif anaTab == cstring "chart":
      if anaChart.isNull:
        p: text "No chart data yet."
      else:
        canvas(id = "lineChart"):
          discard

    # ── Calendar tab ─────────────────────────────────────────────────────────
    elif anaTab == cstring "calendar":
      if anaBreakdown.isNull or calDays.len == 0:
        p: text "No data for this period."
      else:
        article:
          tdiv(class = "cal-grid"):
            for hdr in ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]:
              tdiv(class = "cal-header"): text hdr
            for cd in calDays:
              let cls = if cd.active: cstring "cal-day" else: cstring "cal-day no-data"
              tdiv(class = cls):
                tdiv(class = "cal-day-num"): text cd.label
                if cd.active:
                  if cd.wH > 0:
                    tdiv(class = "cal-bar cal-work"):   discard
                  if cd.cH > 0:
                    tdiv(class = "cal-bar cal-commute"): discard
                  if cd.lH > 0:
                    tdiv(class = "cal-bar cal-lunch"):  discard
                else:
                  text "-"

  # After buildHtml: if on chart tab, attempt to render the chart.
  # On the first call the canvas may not be in the DOM yet (karax hasn't patched).
  # On subsequent calls (triggered by redraw) the canvas will be there.
  if isChartTab and not chartDrawn and not anaChart.isNull:
    if chartExists(cstring "lineChart"):
      chartDrawn = true
      renderChartFromJson(
        cstring "lineChart",
        capLabels, capWork, capCommute, capLunch, capIdle)

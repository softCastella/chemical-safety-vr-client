$ErrorActionPreference = 'Stop'
$outputDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pptxPath = Join-Path $outputDir 'dashboard-data-elements-briefing.pptx'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

function Rgb([int]$r, [int]$g, [int]$b) { return ($r + ($g -shl 8) + ($b -shl 16)) }
function AddText($slide, [string]$text, [float]$x, [float]$y, [float]$w, [float]$h, [float]$size, [int]$color, [bool]$bold = $false) {
    $shape = $slide.Shapes.AddTextbox(1, $x, $y, $w, $h)
    $shape.TextFrame.TextRange.Text = $text
    $shape.TextFrame.WordWrap = -1
    $shape.TextFrame.MarginLeft = 0
    $shape.TextFrame.MarginRight = 0
    $shape.TextFrame.MarginTop = 0
    $shape.TextFrame.MarginBottom = 0
    $shape.TextFrame.TextRange.Font.Name = 'Arial'
    $shape.TextFrame.TextRange.Font.Size = $size
    $shape.TextFrame.TextRange.Font.Bold = [int]$bold
    $shape.TextFrame.TextRange.Font.Color.RGB = $color
    return $shape
}
function AddPanel($slide, [float]$x, [float]$y, [float]$w, [float]$h, [int]$fill, [int]$line = 0) {
    $shape = $slide.Shapes.AddShape(5, $x, $y, $w, $h)
    $shape.Fill.ForeColor.RGB = $fill
    $shape.Line.ForeColor.RGB = $line
    $shape.Line.Weight = 0.75
    return $shape
}
function AddRule($slide, [float]$x1, [float]$y1, [float]$x2, [float]$y2, [int]$color) {
    $shape = $slide.Shapes.AddLine($x1, $y1, $x2, $y2)
    $shape.Line.ForeColor.RGB = $color
    $shape.Line.Weight = 1.2
    return $shape
}

$pp = New-Object -ComObject PowerPoint.Application
$pp.Visible = 1
$presentation = $pp.Presentations.Add()
$presentation.PageSetup.SlideWidth = 540
$presentation.PageSetup.SlideHeight = 960
$slide = $presentation.Slides.Add(1, 12)

$ink = Rgb 22 28 36
$muted = Rgb 85 96 108
$blue = Rgb 45 125 204
$blueLight = Rgb 231 244 252
$gray = Rgb 244 246 248
$rule = Rgb 190 198 207
$white = Rgb 255 255 255
$amber = Rgb 244 169 67

$slide.FollowMasterBackground = 0
AddText $slide '훈련 대시보드 데이터 항목 해설' 42 38 456 70 30 $ink $true | Out-Null
AddText $slide '실제 Unity 세션에서 수집되는 값과 각 값이 의미하는 것' 44 112 450 28 16 $muted $false | Out-Null
AddRule $slide 44 153 496 153 $blue | Out-Null

AddText $slide '한 줄의 CSV = 한 시점의 실제 이벤트' 44 176 450 30 22 $ink $true | Out-Null
AddPanel $slide 44 218 452 66 $blueLight $blue | Out-Null
AddText $slide '사용자 ID  →  세션  →  모드·시나리오  →  단계  →  PPE  →  결과·시간' 60 238 420 28 15 $ink $true | Out-Null
AddText $slide '같은 sessionId로 연결되어 한 사람의 수행 흐름을 추적합니다.' 60 264 420 18 12 $muted $false | Out-Null

AddText $slide '01  사용자·세션' 44 313 452 28 22 $ink $true | Out-Null
AddText $slide 'metaAppScopedUserId   Meta 앱 범위 사용자 식별자\nmetaAgeCategory       Meta가 반환한 연령대(현재 Unknown 가능)\nsessionId              한 번의 플레이를 묶는 식별자\ntimestampUtc           각 이벤트가 발생한 시각\nsession_ended / note   종료 시각과 종료 원인' 44 348 452 104 15 $ink $false | Out-Null

AddText $slide '02  과정·PPE' 44 482 452 28 22 $ink $true | Out-Null
AddText $slide 'mode / workPlan / flowState   교육·훈련·테스트 / 시나리오 / 현재 단계\nitemType                 실제 PPE 종류와 좌·우 구분\ncondition                Clean 또는 Contaminated 상태\n검사 이벤트              PPE 검사 시작·종료와 검사시간\nchoice / result           Use 행동과 승인·거부 결과' 44 517 452 110 15 $ink $false | Out-Null

AddText $slide '03  필수 PPE·음성·시간' 44 660 452 28 22 $ink $true | Out-Null
AddText $slide 'requiredPpeCheck / missingRequiredPpe   필수 목록 완료 여부와 누락 항목\naudioClip                   재생된 교육 음원\naudioLengthSec              음원 파일의 전체 길이\naudioElapsedSec             실제 재생된 시간\ncompleted / stopped / replaced 끝까지 청취·중단·다음 음원으로 교체' 44 695 452 112 15 $ink $false | Out-Null

AddPanel $slide 44 832 216 76 $gray $rule | Out-Null
AddText $slide '계산 가능한 지표' 58 846 190 20 16 $blue $true | Out-Null
AddText $slide '완료율 · 중도 이탈률\n단계별 시간 · PPE 실패율\n음원 청취 완료율' 58 870 190 30 13 $ink $false | Out-Null

AddPanel $slide 280 832 216 76 $blueLight $blue | Out-Null
AddText $slide '추가 계측 필요' 294 846 190 20 16 $blue $true | Out-Null
AddText $slide '그랩 입력·실패 횟수\n강제종료 원인 세분화\n※ 음성 스킵은 그랩에서 제외' 294 870 190 34 13 $ink $false | Out-Null

AddRule $slide 44 923 496 923 $rule | Out-Null
AddText $slide '실제 검증 세션: Education · LeakResponse · Meta ID 기록됨 · 필수 PPE complete 확인' 44 932 452 18 10 $muted $false | Out-Null

$presentation.SaveAs($pptxPath, 24)
$presentation.Close()
$pp.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($slide) | Out-Null
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($presentation) | Out-Null
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($pp) | Out-Null
[GC]::Collect(); [GC]::WaitForPendingFinalizers()
Write-Output $pptxPath

# Mint Cream 아트 적용

2026-11-02 코드·리소스 확인 기준. 로딩은 `TheTenApp.cs`와 `MintTheme.cs`,
재생성은 `MintArtBuilder.cs`와 `HomeArtBuilder.cs`를 기준으로 설명한다.

리소스 위치: `Assets/Resources/Atlas/`

- `MintGarden.png`: 내장 image_gen으로 생성한 정원 배경. 실제 게임에 Resources Sprite로 로드한다.
- `HomeRuins.png`: 사용자 참고 이미지를 바탕으로 imagegen으로 편집한 홈 전용 숲속 유적 및 TEN 로고 배경. 숫자 패, 최고 점수, 문구, 버튼은 실제 UGUI로 표시한다.
- `HomePlay/Trophy/Store/Book/Gear/Chart.png`, `HomeIconFrame.png`, `HomePlayFrame.png`: 코드로 제작한 도트 아이콘과 프레임. HomeArtBuilder가 생성하며 MintCream 아틀라스에 포함한다.
- `ReviveAd.png`, `ReviveFinish.png`, `ReviveTicket.png`: 부활 선택 화면의 16×16 영상 재생·체커 깃발·하트 티켓 아이콘. `RevivePrimaryFrame.png`는 광고/부활권 버튼의 금색 테두리·초록색 24×24 프레임이다.
- `TileIdle.png`, `TileBlocked.png`, `TileSelected.png`: 32×40 도트 패. 크림색 윗면, 민트 측면, 선택 테두리.
- `PanelDark.png`, `PanelCream.png`, `ButtonMint.png`, `BarTrack.png`: 24×24, 9-slice 프레임.
- `MintCream.spriteatlas`: 패/프레임 7종, 홈 아이콘/프레임 8종, 부활 아이콘/프레임 4종을 묶은 Unity SpriteAtlas. 회전/타이트 패킹 없음, Point 필터.
- `MintGlyphs.png`, `MintPixel.fontsettings`, `MintPixel.mat`: 직접 작성한 5×7 영문/숫자 도트 폰트.
  폰트 UV 및 배경 크기를 안정적으로 유지하기 위해 배경과 폰트 텍스처는 UI SpriteAtlas와 별도로 로드한다.

`THE TEN > Build Mint Cream art`로 코드 기반 도트 패/프레임/폰트/아틀라스를 재생성한다.
소스는 `Assets/TheTen/Editor/MintArtBuilder.cs`다. `HomeRuins.png`가 있으면 `HomeArtBuilder.Build()`도 호출해
홈 아이콘과 프레임을 생성하고 아틀라스에 추가한다. 배경 이미지는 이 메뉴에서 생성하지 않는다.
부활 아이콘/프레임도 HomeArtBuilder에서 생성한다. `THE TEN > Build revive icons`로 부활용 4종만 재생성하고 아틀라스에 추가·패킹할 수 있다.
MintPixel은 직접 작성한 도트 폰트이며, 한글 UI에는 별도의 외부 폰트인 Noto Sans CJK KR Regular를 포함한다.
파일은 `Assets/Resources/Fonts/NotoSansCJKkr-Regular.otf`, 라이선스는 같은 폴더의 `OFL.txt`다.
`TheTenApp.KoreanLabel()`은 내장 Noto 폰트를 먼저 로드하고 실패 시 OS 동적 폰트로 대체한다.

스프라이트는 아틀라스에서 먼저 찾고 없으면 `Resources/Atlas/<이름>`의 개별 스프라이트를 로드한다.
MintPixel이 없으면 기본 `LegacyRuntime.ttf` 폰트를 사용한다. 모바일 빌드에는 Atlas 폴더와 Fonts 폴더의 리소스를 함께 포함해야 한다.

보드 위치는 BoardModel이 소유한다. 가림은 같은 더미 ID가 아닌 화면상의 타일 사각형 중첩과 층수로 계산한다.
그리는 순서는 층수 → Y → X이며, 그림자/숫자/팝업은 터치를 받지 않는다.
윗패의 몸체를 터치하면 윗패가 입력을 받으며, 가려진 아랫패의 보이는 조각은 선택되지 않는다.
홈 리뉴얼은 게임 규칙을 변경하지 않는다. 현재 시작 30초, 성공당 +3초, 36장 보드다.
R&D 실험에서는 자유 패를 제거하고 36장 모두를 18/12/6 적층 보드에 배치한다.
패는 동일한 아틀라스를 108×136 크기로 표시하며 숫자도 확대한다. 보드는 하단 공간까지 높이 626을 사용한다.
콤보 팝업은 콤보 슬라이더 중앙에 표시한다. 마지막 조합과 챕터 클리어 팝업에는 큰 도트 폭죽을 추가했다.
기존 30+6 버전의 백업은 RND 폴더에 있다. 현재 코드에는 후속 변경이 있으므로 복구 전
`RND/2026-11-01-no-free-tiles/README.md`의 호환성 안내에 따라 필요한 변경만 선택적으로 반영해야 한다.

부활 선택 화면은 로비의 `HomeButton`을 재사용한다. 720×1280 기준 148×148 아이콘 버튼 3개를 한 줄에 배치하고
WATCH AD / FINISH / REVIVE PASS 라벨을 아래에 표시한다. 부활권 보유 시 첫 버튼은 하트 티켓 아이콘과 USE TICKET 라벨로 바뀐다.
배경과 패널은 기존 MintGarden·PanelDark 리소스를 사용하며, 제목·점수·안내와 버튼은 실제 UGUI로 구성한다.

## 패 모션

`TileVisual`은 게임 모델 좌표를 변경하지 않고 패의 윗면만 최대 10픽셀 들어 올린다.
선택 해제/실패 시 제자리로 내려오고, 바닥 그림자는 위치와 농도로 높이를 표현한다.
빈 공간 클릭/터치 시 선택한 패 전체의 선택 표시와 선택 순서·계산식을 해제한다.
실패 조합도 전체 선택을 해제하며, 타이머에 -3초를 반영하고 `NOT TEN -3 SECONDS` 안내를 표시한다.
10 성공 시 점수/제거 판정은 즉시 반영하고 해당 세 패만 0.48초 동안 확대·축소·상승·페이드한다.
각 패에서 금빛/민트 도트 8개가 2픽셀 간격으로 퍼진다. 패와 반짝임은 즉시 입력 대상에서 제외된다.
새로 개방된 패는 약 0.12초 후 가림 음영이 걷히며 0.4초 안에 밝아진다.
빠른 다음 선택은 허용하며 개방 연출을 단축해 반응한다. 성공할 때마다 전체 보드를 재생성하지 않는다.
연출은 unscaledDeltaTime으로 구동하고 플레이/콤보 시간을 추가로 정지하지 않는다.
보드 교체·셔플은 제거 연출이 끝난 뒤 진행한다. 새 게임/보드 재생성은 남은 연출을 정리한다.

## 배경 생성 프롬프트 (built-in image_gen)

Create a production-ready portrait 9:16 background illustration ONLY for the cute pixel-art mobile number mahjong-solitaire game THE TEN!, Mint Cream art direction. NO TEXT, NO NUMBERS, NO LOGO, NO TILES, NO GAME PIECES, NO UI, NO BUTTONS, NO FRAMES inside center. Handcrafted 16-bit pixel art, visibly crisp square pixel clusters, limited 24-color palette: deep evergreen, moss green, mint, muted sage, ivory daisies with tiny golden centers. View straight down into a cozy walled secret garden with leafy vines framing only the far left/right edges, small pixel daisies and mossy stone lanterns at LOWER corners, subtle ancient stone garden paving at bottom edge. Center 80% of width from 15% down to 90% of height is an EMPTY very subdued soft dark sage-green moss playing surface, flat low-contrast pixel texture, absolutely no distracting props there, suitable behind ivory mahjong tiles that will be composited separately in Unity. Top 15% sparse leafy canopy framing an empty dark green space for a title. Bottom 10% darker moss border with two tiny flowers. The illustration fills the whole canvas edge-to-edge. Charming, calm, nostalgic, premium game environment. Strictly pixel art with hard-edged shading, no blur, no depth of field, no gradients, no realistic materials. Do not draw any gameplay screens, mockups, tile piles, letters, symbols or HUD. Portrait 720x1280 composition.

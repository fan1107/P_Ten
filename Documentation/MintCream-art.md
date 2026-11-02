# Mint Cream 아트 적용

2026-11-02 코드·리소스 확인 기준. 로딩은 `TheTenApp.cs`와 `MintTheme.cs`,
재생성은 `MintArtBuilder.cs`와 `HomeArtBuilder.cs`를 기준으로 설명한다.

리소스 위치: `Assets/Resources/Atlas/`

- `MintGarden.png`: 내장 image_gen으로 생성한 정원 배경. 실제 게임에 Resources Sprite로 로드한다.
- `HomeRuins.png`: 이전 홈의 숲속 유적/TEN 로고 배경 원본. 복구용으로 이미지와 메타 파일을 그대로 보존한다.
- `HomeRuinsMonument.png`: 승인된 시안의 거대한 `10` 석조 구조물을 포함한 새 홈 배경. 기존 배경을 내장 image_gen으로 편집해 별도 파일로 추가했다. 941×1672 크기, Point 필터, 밉맵 없음, 비압축 Sprite로 개별 로드한다. 최고 점수, 안내 문구, 버튼은 실제 UGUI로 표시한다.
- `HomePlay/Trophy/Store/Book/Gear/Chart.png`, `HomeIconFrame.png`, `HomePlayFrame.png`: 코드로 제작한 도트 아이콘과 프레임. HomeArtBuilder가 생성하며 MintCream 아틀라스에 포함한다.
- `ReviveAd.png`, `ReviveFinish.png`, `ReviveTicket.png`: 부활 선택 화면의 16×16 영상 재생·체커 깃발·하트 티켓 아이콘. `RevivePrimaryFrame.png`는 광고/부활권 버튼의 금색 테두리·초록색 24×24 프레임이다.
- `TileIdle.png`, `TileBlocked.png`, `TileSelected.png`: 32×40 도트 패. 크림색 윗면, 민트 측면, 선택 테두리.
- `PanelDark.png`, `PanelCream.png`, `ButtonMint.png`, `BarTrack.png`: 24×24, 9-slice 프레임.
- `MintCream.spriteatlas`: 패/프레임 7종, 홈 아이콘/프레임 8종, 부활 아이콘/프레임 4종을 묶은 Unity SpriteAtlas. 회전/타이트 패킹 없음, Point 필터.
- `MintGlyphs.png`, `MintPixel.fontsettings`, `MintPixel.mat`: 이전 5×7 도트 폰트 원본. 보존하지만 현재 UI에서는 사용하지 않는다.
  배경과 폰트 텍스처는 UI SpriteAtlas와 별도로 로드한다.

`THE TEN > Build Mint Cream art`로 코드 기반 도트 패/프레임/폰트/아틀라스를 재생성한다.
소스는 `Assets/TheTen/Editor/MintArtBuilder.cs`다. `HomeRuins.png`가 있으면 `HomeArtBuilder.Build()`도 호출해
홈 아이콘과 프레임을 생성하고 아틀라스에 추가한다. 배경 이미지는 이 메뉴에서 생성하지 않는다.
부활 아이콘/프레임도 HomeArtBuilder에서 생성한다. `THE TEN > Build revive icons`로 부활용 4종만 재생성하고 아틀라스에 추가·패킹할 수 있다.
현재 글자와 숫자는 모두 TextMeshPro의 Noto Sans Bold SDF로 표시한다. 배경·아이콘·패널은 기존 도트 리소스와 Point 필터를 유지한다.
`Assets/Resources/Fonts/NotoSans-Bold.ttf`는 [공식 Noto 저장소](https://github.com/notofonts/noto-fonts/tree/main/hinted/ttf/NotoSans)에서 가져왔으며 같은 폴더의 `NotoSans-LICENSE.txt`에 OFL 라이선스를 포함한다.
`NotoSans-Bold SDF.asset`는 영문·숫자·게임 기호를 미리 구운 SDF 아틀라스다. 한글은 기존 `NotoSansCJKkr-Regular.otf`에서 생성한 `NotoSans-Korean SDF.asset` 폴백을 사용한다. 한글 원본의 라이선스는 `OFL.txt`다.
`NotoSans-Outline.mat`는 밝은 글자용 짙은 초록 외곽선(폭 0.14), `NotoSans-TileOutline.mat`는 어두운 숫자·버튼 글자용 아이보리 외곽선(폭 0.09)이다. 두 머티리얼은 TMP Mobile Distance Field 셰이더를 사용한다.
Regular 원본을 쓰는 한글 폴백은 SDF 기본 획 두께를 0.75로 보정해 외곽선 안쪽의 가는 획도 보이도록 한다. 글자 간격은 늘리지 않는다.
`UiTypography`가 공용 폰트/머티리얼을 연결한다. 크기 애니메이션은 SDF로 렌더링하고 한글 폴백도 동일한 머티리얼 설정을 따른다. OS 폰트에 의존하지 않는다.
`THE TEN > Build Noto Sans Typography`는 폰트 에셋이 없으면 생성하고 머티리얼 설정을 갱신한다. TMP Essential Resources를 먼저 임포트해야 한다. 코드는 `Assets/TheTen/Editor/UiTypographyBuilder.cs`다.

스프라이트는 아틀라스에서 먼저 찾고 없으면 `Resources/Atlas/<이름>`의 개별 스프라이트를 로드한다.
모바일 빌드에는 Atlas/Fonts 리소스와 `Assets/TextMesh Pro`의 TMP 필수 리소스를 함께 포함한다.

보드 위치는 BoardModel이 소유한다. 가림은 같은 더미 ID가 아닌 화면상의 타일 사각형 중첩과 층수로 계산한다.
그리는 순서는 층수 → Y → X이며, 그림자/숫자/팝업은 터치를 받지 않는다.
윗패의 몸체를 터치하면 윗패가 입력을 받으며, 가려진 아랫패의 보이는 조각은 선택되지 않는다.
홈 리뉴얼은 게임 규칙을 변경하지 않는다. 현재 시작 30초, 성공당 +3초, 36장 보드다.
홈 중앙의 2·3·5 패, `MAKE ROOM FOR TEN.` 및 아래 설명은 제거했다. 장르 문구 아래에 36 크기의 `3 Touch, Make 10!`를 한 줄로 표시한다.
`LetterWave`를 게임 화면과 공유하며 `3`은 노랑, `10`은 빨강으로 한 글자씩 최대 1.22배 확대한다. 설정의 `MENU MOTION`은 이제 홈 안내 문구의 파도 연출을 켜고 끈다.
이전 배경으로 돌아가려면 `HomeMenu.cs`의 배경 리소스 이름을 `HomeRuins`로 바꾼다. 원본 배경에는 `10` 구조물이 없으며, 과거 문구와 세 숫자 패널은 별도의 UI 코드다.
R&D 실험에서는 자유 패를 제거하고 36장 모두를 18/12/6 적층 보드에 배치한다.
패는 동일한 아틀라스를 108×136 크기로 표시하며 숫자도 확대한다. 보드는 하단 공간까지 높이 626을 사용한다.
콤보는 슬라이더 왼쪽의 152×54 둥근 배지로 표시한다. 배지와 바의 세로 중심은 Y=343으로 같고 배지를 앞에 그린다.
콤보 바를 기존보다 16픽셀 내려 시간바와의 겹침을 없앴다. 채움 두께가 커져도 중심은 고정되며 배지가 최대 1.16배 확대되어도 시간바와 닿지 않는다.
`RoundedBadgeGraphic`이 끝이 둥근 테두리/면을 메시로 그리며, 별도 텍스처는 필요 없다. 배지는 입력을 받지 않는다.
×1/×1.5 아이보리 → ×2/×2.5 녹색 → ×3 파랑 → ×4 노랑 → ×5 빨강 순서이며 최대 표기는 `×5!!`다.
콤보 상승은 최대 1.16배, 중앙 점수 증가는 최대 1.14배로 한 번 확대한다. `ScalePulse`가 0.36초 안에 원래 크기로 돌린다.
큰 콤보 팝업은 제거했다. 마지막 조합과 챕터 클리어 팝업의 도트 폭죽은 유지한다.
시간바 채움은 22픽셀, 보라색 콤보 바는 14픽셀(성공 직후 최대 16픽셀) 높이다.
시간바는 생명 시작 시간 대비 잔여 비율에 따라 녹색(#77C668) → 노랑(#F4C84F) → 빨강(#EE6556)으로 바뀐다.
콤보 바는 배율과 관계없이 보라색(#B279ED)을 유지한다. 바 아래 조합식·작은 상태 문구를 제거하고
노란색(#F6D15F) 점수 숫자만 60 크기로 중앙 정렬한다. 점수 영역 너비는 640이며 천 단위 쉼표를 표시한다. 타이머는 64, 숫자 패는 56 크기다.
챕터 아래에 `LetterWave`로 `3 Touch, Make 10!`를 표시한다. 28 크기의 글자가 0.075초 간격으로 최대 1.22배 확대되며 3.6초마다 반복한다.
`3`은 노랑(#F6D15F), `10`은 붉은색(#F17468)이다. 하단 제거 수는 숨기고 최고 점수만 42 크기로 중앙 정렬한다.
기존 30+6 버전의 백업은 RND 폴더에 있다. 현재 코드에는 후속 변경이 있으므로 복구 전
`RND/2026-11-01-no-free-tiles/README.md`의 호환성 안내에 따라 필요한 변경만 선택적으로 반영해야 한다.

부활 선택 화면은 로비의 `HomeButton`을 재사용한다. 720×1280 기준 148×148 아이콘 버튼 3개를 한 줄에 배치하고
WATCH AD / FINISH / REVIVE PASS 라벨을 아래에 표시한다. 부활권 보유 시 첫 버튼은 하트 티켓 아이콘과 USE TICKET 라벨로 바뀐다.
배경과 패널은 기존 MintGarden·PanelDark 리소스를 사용하며, 제목·점수·안내와 버튼은 실제 UGUI로 구성한다.

## 패 모션

`TileVisual`은 게임 모델 좌표를 변경하지 않고 패의 윗면만 최대 10픽셀 들어 올린다.
선택 해제/실패 시 제자리로 내려오고, 바닥 그림자는 위치와 농도로 높이를 표현한다.
빈 공간 클릭/터치 시 선택한 패 전체의 선택 표시와 선택 순서를 해제한다.
선택한 패 하단의 01/02 순서 숫자는 생성하지 않는다. 선택 테두리와 10픽셀 들림은 유지한다.
실패 조합도 전체 선택을 해제하며, 타이머에 -3초를 반영하고 `NOT TEN -3 SECONDS` 안내를 표시한다.
실패한 세 패는 0.44초 동안 붉게 표시되고 최대 5픽셀 범위에서 좌우로 흔들린 뒤 원래 색·위치로 돌아온다.
카운트다운 숫자도 동시에 붉어지며 한 번만 최대 1.14배로 커졌다 원래 크기로 돌아온다.
특수 패의 고유 색과 10초 이하의 시간 경고색은 연출 종료 후 복원된다. 반복 실패는 연출을 다시 시작하며,
다음 선택·취소 입력은 즉시 가능하다. 새 게임은 카운트다운 연출을 초기화하고, 성공한 패는 성공 연출로 전환한다.
10 성공 시 점수/제거 판정은 즉시 반영하고 해당 세 패만 0.48초 동안 확대·축소·상승·페이드한다.
각 패에서 금빛/민트 도트 8개가 2픽셀 간격으로 퍼진다. 패와 반짝임은 즉시 입력 대상에서 제외된다.
새로 개방된 패는 약 0.12초 후 가림 음영이 걷히며 0.4초 안에 밝아진다.
빠른 다음 선택은 허용하며 개방 연출을 단축해 반응한다. 성공할 때마다 전체 보드를 재생성하지 않는다.
연출은 unscaledDeltaTime으로 구동하고 플레이/콤보 시간을 추가로 정지하지 않는다.
보드 교체·셔플은 제거 연출이 끝난 뒤 진행한다. 새 게임/보드 재생성은 남은 연출을 정리한다.

## 로비 10 구조물 편집 프롬프트 (built-in image_gen)

입력은 보존된 `HomeRuins.png`이며, 승인된 결과 원본을 `HomeRuinsMonument.png`로 복사했다. 아래는 실제 사용한 편집 프롬프트다.

Use case: precise-object-edit. Edit the provided portrait pixel-art game lobby background, not a different scene. Input image: HomeRuins.png is the edit target. Preserve its exact 9:16 framing, large cream TEN logo with flowers in the upper third, hanging foliage, cyan sky, side ruined walls, stone plinth, pond, palette, lighting and crisp pixel clusters. Add one monumental sculptural numeral '10' standing firmly on the existing central stone plinth. This replaces the location of the three small game tiles in the composed UI, so there must be NO small numbered tiles. The 10 is actual ancient architecture: an unmistakable chunky stone numeral 1 with a cap and base on the left, and a massive open-center stone numeral 0 on the right, both equal height, carved from warm pale gray limestone blocks, cracks, chipped stepped edges, patches of moss, a few winding vines and tiny ivory daisies matching the existing ruins. The zero is a thick rounded-rectangular arch with the dark forest visible through its opening. The 10 should feel weighty, enormous and integrated with this environment, with dimensional side faces and grounded shadows; preserve a clear silhouette, no luminous magic, no floating type, no flat graphic digits. Main new monument silhouette spans approximately 25% to 75% of image width and 44% to 65% of image height, its feet resting on the existing platform. Leave the dark green area between 35% and 43% of image height clean and quiet so a single UI tagline can be overlaid later. Preserve the platform front and water below 66% height for existing score and menu overlays. Original TEN logo stays unchanged. Do not add any other lettering, captions, numbers, UI panels or buttons. No MAKE ROOM FOR TEN, no explanatory paragraphs. Match original handcrafted 16-bit pixel-art style with sharp square pixels, no smooth vector rendering and no photorealism. This is a background asset for a lobby mockup, with the new colored animated tagline composited separately.

## 게임 배경 생성 프롬프트 (built-in image_gen)

Create a production-ready portrait 9:16 background illustration ONLY for the cute pixel-art mobile number mahjong-solitaire game THE TEN!, Mint Cream art direction. NO TEXT, NO NUMBERS, NO LOGO, NO TILES, NO GAME PIECES, NO UI, NO BUTTONS, NO FRAMES inside center. Handcrafted 16-bit pixel art, visibly crisp square pixel clusters, limited 24-color palette: deep evergreen, moss green, mint, muted sage, ivory daisies with tiny golden centers. View straight down into a cozy walled secret garden with leafy vines framing only the far left/right edges, small pixel daisies and mossy stone lanterns at LOWER corners, subtle ancient stone garden paving at bottom edge. Center 80% of width from 15% down to 90% of height is an EMPTY very subdued soft dark sage-green moss playing surface, flat low-contrast pixel texture, absolutely no distracting props there, suitable behind ivory mahjong tiles that will be composited separately in Unity. Top 15% sparse leafy canopy framing an empty dark green space for a title. Bottom 10% darker moss border with two tiny flowers. The illustration fills the whole canvas edge-to-edge. Charming, calm, nostalgic, premium game environment. Strictly pixel art with hard-edged shading, no blur, no depth of field, no gradients, no realistic materials. Do not draw any gameplay screens, mockups, tile piles, letters, symbols or HUD. Portrait 720x1280 composition.

# Mint Cream 아트 적용

리소스 위치: `Assets/Resources/Atlas/`

- `MintGarden.png`: 내장 image_gen으로 생성한 정원 배경. 실제 게임에 Resources Sprite로 로드한다.
- `TileIdle.png`, `TileBlocked.png`, `TileSelected.png`: 32×40 도트 패. 크림색 윗면, 민트 측면, 선택 테두리.
- `PanelDark.png`, `PanelCream.png`, `ButtonMint.png`, `BarTrack.png`: 24×24, 9-slice 프레임.
- `MintCream.spriteatlas`: 위 7종 UI 스프라이트를 묶은 Unity SpriteAtlas. 회전/타이트 패킹 없음, Point 필터.
- `MintGlyphs.png`, `MintPixel.fontsettings`, `MintPixel.mat`: 직접 작성한 5×7 영문/숫자 도트 폰트.
  폰트 UV 및 배경 크기를 안정적으로 유지하기 위해 배경과 폰트 텍스처는 UI SpriteAtlas와 별도로 로드한다.

`THE TEN > Build Mint Cream art`로 코드 기반 도트 패/프레임/폰트/아틀라스를 재생성한다.
소스는 `Assets/TheTen/Editor/MintArtBuilder.cs`다. 외부 폰트나 유료 에셋을 사용하지 않았다.
아트 리소스가 없는 경우 기본 폰트 폴백만 있으므로 모바일 빌드에는 Atlas 폴더 전체를 포함해야 한다.

보드 위치는 BoardModel이 소유한다. 가림은 같은 더미 ID가 아닌 화면상의 타일 사각형 중첩과 층수로 계산한다.
그리는 순서는 층수 → Y → X이며, 그림자/숫자/팝업은 터치를 받지 않는다.
윗패의 몸체를 터치하면 윗패가 입력을 받으며, 가려진 아랫패의 보이는 조각은 선택되지 않는다.
기존 점수, 3초 콤보, 36장, 40초, 부활권 규칙은 유지한다.

## 패 모션

`TileVisual`은 게임 모델 좌표를 변경하지 않고 패의 윗면만 최대 10픽셀 들어 올린다.
선택 해제/실패 시 제자리로 내려오고, 바닥 그림자는 위치와 농도로 높이를 표현한다.
10 성공 시 점수/제거 판정은 즉시 반영하고 해당 세 패만 0.48초 동안 확대·축소·상승·페이드한다.
각 패에서 금빛/민트 도트 8개가 2픽셀 간격으로 퍼진다. 패와 반짝임은 즉시 입력 대상에서 제외된다.
새로 개방된 패는 약 0.12초 후 가림 음영이 걷히며 0.4초 안에 밝아진다.
빠른 다음 선택은 허용하며 개방 연출을 단축해 반응한다. 성공할 때마다 전체 보드를 재생성하지 않는다.
연출은 unscaledDeltaTime으로 구동하고 플레이/콤보 시간을 추가로 정지하지 않는다.
보드 교체·셔플은 제거 연출이 끝난 뒤 진행한다. 새 게임/보드 재생성은 남은 연출을 정리한다.

## 배경 생성 프롬프트 (built-in image_gen)

Create a production-ready portrait 9:16 background illustration ONLY for the cute pixel-art mobile number mahjong-solitaire game THE TEN!, Mint Cream art direction. NO TEXT, NO NUMBERS, NO LOGO, NO TILES, NO GAME PIECES, NO UI, NO BUTTONS, NO FRAMES inside center. Handcrafted 16-bit pixel art, visibly crisp square pixel clusters, limited 24-color palette: deep evergreen, moss green, mint, muted sage, ivory daisies with tiny golden centers. View straight down into a cozy walled secret garden with leafy vines framing only the far left/right edges, small pixel daisies and mossy stone lanterns at LOWER corners, subtle ancient stone garden paving at bottom edge. Center 80% of width from 15% down to 90% of height is an EMPTY very subdued soft dark sage-green moss playing surface, flat low-contrast pixel texture, absolutely no distracting props there, suitable behind ivory mahjong tiles that will be composited separately in Unity. Top 15% sparse leafy canopy framing an empty dark green space for a title. Bottom 10% darker moss border with two tiny flowers. The illustration fills the whole canvas edge-to-edge. Charming, calm, nostalgic, premium game environment. Strictly pixel art with hard-edged shading, no blur, no depth of field, no gradients, no realistic materials. Do not draw any gameplay screens, mockups, tile piles, letters, symbols or HUD. Portrait 720x1280 composition.

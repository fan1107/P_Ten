# THE TEN! 로컬 Android APK

2026-11-02 12:18 KST에 최신 로비·콤보·점수·페널티 UI를 포함한 APK를 빌드했다.
Unity MCP 빌드 작업 `build-cd609317b1`은 355.2초 후 성공했다. 오류 0건, 경고 21건이다.
경고는 Unity 패키지의 셰이더 정밀도 변환 18건, 진단용 디버그 심볼 설정 1건, 빌드 중 일시적인 MCP 연결 종료 2건이다.
소스 코드 컴파일 오류는 없으며 MCP 연결도 복구됐다.

설치 파일은 59,878,906 bytes(약 59.9 MB)다. BuildReport의 전체 산출물 크기 930.03 MB는 보조 파일까지 포함한 값으로 APK 파일 크기와 다르다.
SDK 도구로 APK v2 서명, zipalign의 16KB 네이티브 라이브러리 페이지 정렬 검사, 앱 ID/버전/SDK/ARM64 및 GameActivity 실행 진입점 1개를 확인했다.
INTERNET/ACCESS_NETWORK_STATE 권한은 없다. 패키징 보고서에서 새 `HomeRuinsMonument.png`와 보존된 `HomeRuins.png` 모두 포함된 것을 확인했다.
실제 Android 기기 설치 및 실행은 아직 수행하지 않았다. `Builds/`는 Git 추적 제외 대상이다.

- 출력 경로: `Builds/Android/THE-TEN-Local.apk`
- SHA-256: `E94B19B61EF1DCE99013844E3E5BCBD31249729CB5C449B9AAF3C7313F61F3A7`
- 앱 ID: `com.theten.local`, 버전 `0.1.0` / 코드 `1`
- Android 8.0(API 26) 이상, 타깃 API 35, ARM64. IL2CPP, 세로 화면.
- 개발 서버 없이 실행한다. 최고 점수와 테스트 구매 상태는 기기 내부 PlayerPrefs에 저장한다.
- 인터넷 및 네트워크 상태 권한을 AndroidManifest에서 제거한다.
- 실제 결제·광고·세계/국가 순위 서버는 연결하지 않는다. 상점과 광고는 MOCK 테스트 동작이며 순위 화면은 UI 미리보기다.
- 일일 부활권의 MOCK 시계는 보안 서버 시간이 아니며 정식 결제용으로 사용할 수 없다.
- 기본 테스트 서명으로 생성한다. 스토어 배포용 서명은 별도 준비한다.

## 재빌드

Unity에서 `TheTen.Editor.LocalApkBuild.Configure()`를 실행한 후
Android 대상으로 `Assets/Scenes/TheTen.unity`를 빌드하고 저장 위치를 `Builds/Android/THE-TEN-Local.apk`로 지정한다.
`Configure()`는 설정을 저장하는 메서드이며 직접 APK를 생성하지 않는다. Build Settings의 씬 목록을 TheTen 하나로 설정하고,
APK 출력, 비개발 빌드, 기본 테스트 서명, `EnableMockCommerce=true`를 지정한다.
ARM64/IL2CPP와 Android SDK/NDK/OpenJDK 모듈이 필요하다.

## 포함 폰트

한글 표시를 위해 Noto Sans CJK KR Regular를 앱 안에 포함한다. `TheTenApp.KoreanLabel()`이
내장 폰트를 먼저 로드하고, 실패한 경우에만 OS 동적 폰트로 대체한다.
공식 소스: https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Korean
라이선스: SIL Open Font License 1.1 (`Assets/Resources/Fonts/OFL.txt`).
폰트 파일은 `Assets/Resources/Fonts/NotoSansCJKkr-Regular.otf`에 있으며, 임포트 설정의 `includeFontData`는 1이다.

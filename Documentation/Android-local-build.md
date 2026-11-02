# THE TEN! 로컬 Android APK

2026-11-02 `LocalApkBuild.cs`, `ProjectSettings.asset`, `EditorBuildSettings.asset`, AndroidManifest 및 폰트 리소스를 대조했다.
아래 빌드 결과는 기존 검증 기록이며, 이번 문서 점검에서 APK를 다시 빌드하거나 서명을 검증한 것은 아니다.

2026-11-01 빌드 성공 기록은 `Documentation/Validation.json`의 `androidLocalApk`에 있다.
기록상 오류 0건·경고 2건, APK 61,260,813 bytes, APK v2 서명 검증 통과, 실행 진입점 1개, 인터넷 권한 없음이다.
현재 AndroidManifest는 GameActivity를 진입점으로 선언하며 INTERNET/ACCESS_NETWORK_STATE 권한을 모두 제거하도록 설정되어 있다.
2026-11-02 확인 시 `Builds/Android/THE-TEN-Local.apk`는 작업 폴더에 없다. `Builds/`는 Git 추적 제외 대상이다.
실제 Android 기기 설치 및 실행은 검증 기록상 미수행 상태다.

- 기록된 출력 경로 / 재빌드 저장 위치: `Builds/Android/THE-TEN-Local.apk`
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

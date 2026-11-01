# THE TEN! 로컬 Android APK

2026-11-01 최종 빌드 성공. APK 61,260,813 bytes, APK v2 서명 검증 통과.
실행 진입점은 GameActivity 하나이며 INTERNET/ACCESS_NETWORK_STATE 권한이 없음을 aapt로 확인했다.
연결된 기기가 없어 실기기 설치 및 실행 검증은 하지 못했다.

- 출력: `Builds/Android/THE-TEN-Local.apk`
- 앱 ID: `com.theten.local`, 버전 `0.1.0` / 코드 `1`
- Android 8.0(API 26) 이상, ARM64. IL2CPP, 세로 화면.
- 개발 서버 없이 실행한다. 최고 점수와 테스트 구매 상태는 기기 내부 PlayerPrefs에 저장한다.
- 인터넷 및 네트워크 상태 권한을 AndroidManifest에서 제거한다.
- 실제 결제·광고·세계/국가 순위 서버는 연결하지 않는다. 상점과 광고는 MOCK 테스트 동작이며 순위 화면은 UI 미리보기다.
- 일일 부활권의 MOCK 시계는 보안 서버 시간이 아니며 정식 결제용으로 사용할 수 없다.
- 기본 테스트 서명으로 생성한다. 스토어 배포용 서명은 별도 준비한다.

## 재빌드

Unity에서 `TheTen.Editor.LocalApkBuild.Configure()`를 실행한 후
Android 대상으로 `Assets/Scenes/TheTen.unity`를 APK로 빌드한다.
ARM64/IL2CPP와 Android SDK/NDK/OpenJDK 모듈이 필요하다.

## 포함 폰트

한글 표시를 위해 Noto Sans CJK KR Regular를 앱 안에 포함한다.
공식 소스: https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Korean
라이선스: SIL Open Font License 1.1 (`Assets/Resources/Fonts/OFL.txt`).
폰트 파일은 `Assets/Resources/Fonts/NotoSansCJKkr-Regular.otf`에 저장한다.

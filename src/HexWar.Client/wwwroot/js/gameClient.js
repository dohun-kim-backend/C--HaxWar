// wwwroot/js/gameClient.js

/**
 * gRPC + WebSocket 게임 클라이언트
 */
class GameClient {
    constructor() {
        this.ws = null;
        this.playerId = null;
        this.playerSide = null;
        this.roomId = null;
        this.onStateUpdate = null;
        this.onEncounter = null;
        this.onGameOver = null;
        this.onLog = null;
        this.onConnectionChange = null;
        
        // 재연결 및 시퀀스 보정 상태
        this.lastSeenSequence = 0;
        this.reconnectTimeout = null;
        this.isClosedIntentionally = false;
    }

    /**
     * gRPC-Web 매치메이킹
     */
    async findMatch(playerName) {
        this.playerId = playerName || 'player-' + Math.random().toString(36).substr(2, 6);

        // gRPC-Web 클라이언트
        const { MatchmakingServiceClient, JoinQueueRequest } = await import('./generated/matchmaking_bundle.js');

        const client = new MatchmakingServiceClient(window.location.origin);
        const request = new JoinQueueRequest();
        request.setPlayerId(this.playerId);
        request.setRating(1500);

        return new Promise((resolve, reject) => {
            const stream = client.joinQueue(request);

            stream.on('data', (update) => {
                const status = update.getStatus();

                if (status === 1) { // MATCHED
                    const result = update.getMatchResult();
                    this.roomId = result.getRoomId();
                    this.playerSide = result.getPlayerSide();

                    resolve({
                        roomId: this.roomId,
                        playerSide: this.playerSide,
                        opponentId: result.getOpponentId()
                    });
                }
            });

            stream.on('error', (err) => {
                reject(err);
            });
        });
    }

    /**
     * WebSocket 연결
     */
    connect() {
        if (this.reconnectTimeout) {
            clearTimeout(this.reconnectTimeout);
            this.reconnectTimeout = null;
        }
        
        this.isClosedIntentionally = false;
        
        const wsUrl = `ws://${window.location.host}/ws/game/${this.roomId}/${this.playerSide}`;
        this.ws = new WebSocket(wsUrl);

        this.ws.onopen = () => {
            this.log('서버 연결됨');
            if (this.onConnectionChange) this.onConnectionChange(true);
            
            // 재연결 시 누락된 이벤트 동기화 요청
            if (this.lastSeenSequence > 0) {
                this.log(`누락된 이벤트 동기화 요청 (마지막 시퀀스: ${this.lastSeenSequence})`);
                this.send({
                    type: 'reconnect_sync',
                    payload: {
                        last_seen_sequence: this.lastSeenSequence
                    }
                });
            } else {
                this.send({ type: 'get_state' });
            }
        };

        this.ws.onmessage = (event) => {
            const message = JSON.parse(event.data);
            
            // 시퀀스 번호 추적
            if (message.sequence) {
                this.lastSeenSequence = Math.max(this.lastSeenSequence, message.sequence);
            }
            
            this.handleMessage(message);
        };

        this.ws.onclose = () => {
            this.log('서버 연결 끊김');
            if (this.onConnectionChange) this.onConnectionChange(false);
            
            // 의도적으로 종료한 것이 아닐 때만 재연결 시도
            if (!this.isClosedIntentionally) {
                this.reconnectTimeout = setTimeout(() => {
                    this.log('서버 재연결 시도 중...');
                    this.connect();
                }, 3000);
            }
        };

        this.ws.onerror = (error) => {
            this.log('연결 오류: ' + error.message);
        };
    }

    /**
     * 서버 메시지 처리
     */
    handleMessage(message) {
        switch (message.type) {
            case 'state_update':
                if (this.onStateUpdate) {
                    this.onStateUpdate(message.payload);
                }
                break;

            case 'game_event':
                this.handleGameEvent(message);
                break;

            case 'error':
                this.log('❌ 오류: ' + message.payload?.message);
                break;

            case 'pong':
                // 연결 유지 확인
                break;
        }
    }

    handleGameEvent(message) {
        const eventType = message.eventType;
        const payload = message.payload;

        switch (eventType) {
            case 'GameStarted':
                this.log('🎮 게임 시작!');
                break;

            case 'RoundStarted':
                this.log(`📍 라운드 ${message.round} 시작 - 계획 단계`);
                break;

            case 'UnitsDeparted':
                this.log(`🚀 ${payload.side}측 ${payload.fromNode.value}번 노드에서 ${payload.unitCount}기 출발`);
                break;

            case 'UnitsArrived':
                this.log(`✅ ${payload.side}측 유닛 ${payload.unitCount}기 ${payload.destinationNode.value}번 노드 도착`);
                break;

            case 'EncounterOccurred':
                this.log(`⚡ 조우 발생! 간선 ${payload.fromNode}-${payload.toNode}`);
                if (this.onEncounter) {
                    this.onEncounter(payload);
                }
                break;

            case 'EncounterResolved':
                this.log(`🔨 조우 해소: A=${payload.decisionA}, B=${payload.decisionB}`);
                break;

            case 'RoundResolved':
                this.log(`🔄 라운드 ${payload.completedRound} 해소 완료`);
                // 상태 갱신 요청
                this.send({ type: 'get_state' });
                break;

            case 'GameOver':
                const winner = payload.winner ? payload.winner : '무승부';
                this.log(`🏆 게임 종료! 승자: ${winner}`);
                if (this.onGameOver) {
                    this.onGameOver(payload);
                }
                break;
        }
    }

    /**
     * 서버로 메시지 전송
     */
    send(message) {
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(message));
        }
    }

    /**
     * 유닛 이동 명령
     */
    moveUnits(fromNode, toNode, count) {
        this.send({
            type: 'move_units',
            payload: {
                from: fromNode,
                to: toNode,
                count: count
            }
        });
    }

    /**
     * 조우 결정
     */
    resolveEncounter(fromNode, toNode, decision) {
        this.send({
            type: 'encounter_decision',
            payload: {
                from_node: fromNode,
                to_node: toNode,
                decision: decision
            }
        });
    }

    /**
     * 상태 조회
     */
    getState() {
        this.send({ type: 'get_state' });
    }

    log(message) {
        console.log('[Game]', message);
        if (this.onLog) this.onLog(message);
    }

    disconnect() {
        this.isClosedIntentionally = true;
        if (this.reconnectTimeout) {
            clearTimeout(this.reconnectTimeout);
            this.reconnectTimeout = null;
        }
        if (this.ws) {
            this.ws.close();
        }
    }
}
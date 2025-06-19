from flask import Flask, request, jsonify
from flask_cors import CORS
import psycopg2
from psycopg2.extras import RealDictCursor

app = Flask(__name__)
CORS(app)

DB_PARAMS = {
    'dbname': 'testdb',
    'user': 'testuser',
    'password': 'testpass',
    'host': 'localhost',  # e.g., 'localhost' or Render DB hostname
    'port': 5432,         # default PostgreSQL port
}

def get_conn():
    return psycopg2.connect(**DB_PARAMS)

@app.route('/submit', methods=['POST'])
def submit_score():
    data = request.get_json()
    name = data.get('player')
    score = data.get('score')

    if not name or not isinstance(score, int):
        return jsonify({'status': 'error', 'message': 'Invalid data'}), 400

    try:
        conn = get_conn()
        cur = conn.cursor()
        cur.execute('INSERT INTO highscores (player, score) VALUES (%s, %s)', (name, score))
        conn.commit()
        cur.close()
        conn.close()
        return jsonify({'status': 'success'}), 200
    except Exception as e:
        return jsonify({'status': 'error', 'message': str(e)}), 500

@app.route('/leaderboard', methods=['GET'])
def get_leaderboard():
    try:
        conn = get_conn()
        cur = conn.cursor(cursor_factory=RealDictCursor)
        cur.execute('SELECT player, score FROM highscores ORDER BY score DESC LIMIT 10')
        rows = cur.fetchall()
        cur.close()
        conn.close()
        return jsonify(rows)
    except Exception as e:
        return jsonify({'status': 'error', 'message': str(e)}), 500

@app.route('/rank/<player>', methods=['GET'])
def get_rank(player):
    try:
        conn = get_conn()
        cur = conn.cursor()
        cur.execute('''
            SELECT COUNT(*) + 1 FROM highscores WHERE score > (
                SELECT score FROM highscores WHERE player = %s ORDER BY score DESC LIMIT 1
            )
        ''', (player,))
        result = cur.fetchone()
        cur.close()
        conn.close()
        if result and result[0]:
            return jsonify({'rank': result[0]})
        return jsonify({'message': 'Player not found'}), 404
    except Exception as e:
        return jsonify({'status': 'error', 'message': str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)

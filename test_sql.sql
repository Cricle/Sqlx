CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    age INTEGER NOT NULL
);

INSERT INTO users (name, age) VALUES ('User1', 25);
INSERT INTO users (name, age) VALUES ('User2', 25);
INSERT INTO users (name, age) VALUES ('User3', 30);
INSERT INTO users (name, age) VALUES ('User4', 30);
INSERT INTO users (name, age) VALUES ('User5', 35);

SELECT DISTINCT age FROM users ORDER BY age;

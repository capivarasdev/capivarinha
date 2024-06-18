CREATE TABLE user_message (
    user_id TEXT NOT NULL,
    message_content_length INTEGER NOT NULL
);

CREATE VIEW user_average_message_content_length AS
    SELECT user_id, AVG(message_content_length) AS average_content_length
    FROM user_message
    GROUP BY user_id

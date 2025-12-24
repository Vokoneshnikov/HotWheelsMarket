CREATE TABLE users (
                       id SERIAL PRIMARY KEY,
                       username VARCHAR(50) UNIQUE NOT NULL,
                       email VARCHAR(100) UNIQUE NOT NULL,
                       password_hash TEXT NOT NULL,
                       first_name VARCHAR(50),
                       last_name VARCHAR(50),
                       balance NUMERIC(12,2) DEFAULT 0,
                       created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE roles (
                       id SERIAL PRIMARY KEY,
                       name VARCHAR(20) UNIQUE NOT NULL
);

CREATE TABLE user_roles (
                            user_id INT REFERENCES users(id) ON DELETE CASCADE,
                            role_id INT REFERENCES roles(id) ON DELETE CASCADE,
                            PRIMARY KEY (user_id, role_id)
);

CREATE TABLE cars (
                      id SERIAL PRIMARY KEY,
                      owner_id INT REFERENCES users(id) ON DELETE SET NULL,
                      name VARCHAR(100) NOT NULL,
                      description TEXT,
                      status VARCHAR(20) DEFAULT 'available',
                      created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE car_photos (
                            id SERIAL PRIMARY KEY,
                            car_id INT REFERENCES cars(id) ON DELETE CASCADE,
                            photo_url TEXT NOT NULL,
                            uploaded_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE auctions (
                          id SERIAL PRIMARY KEY,
                          car_id INT UNIQUE REFERENCES cars(id) ON DELETE CASCADE,
                          seller_id INT REFERENCES users(id) ON DELETE SET NULL,
                          start_price NUMERIC(12,2) NOT NULL,
                          bid_step NUMERIC(12,2) DEFAULT 100,
                          current_bid NUMERIC(12,2) DEFAULT 0,
                          current_bidder_id INT REFERENCES users(id),
                          status VARCHAR(20) DEFAULT 'active',
                          started_at TIMESTAMP DEFAULT NOW(),
                          ends_at TIMESTAMP NOT NULL
);

CREATE TABLE bids (
                      id SERIAL PRIMARY KEY,
                      auction_id INT REFERENCES auctions(id) ON DELETE CASCADE,
                      bidder_id INT REFERENCES users(id) ON DELETE CASCADE,
                      amount NUMERIC(12,2) NOT NULL,
                      created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE market_listings (
                                 id SERIAL PRIMARY KEY,
                                 car_id INT UNIQUE REFERENCES cars(id) ON DELETE CASCADE,
                                 seller_id INT REFERENCES users(id),
                                 price NUMERIC(12,2) NOT NULL,
                                 status VARCHAR(20) DEFAULT 'active',
                                 created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE transactions (
                              id SERIAL PRIMARY KEY,
                              buyer_id INT REFERENCES users(id),
                              seller_id INT REFERENCES users(id),
                              car_id INT REFERENCES cars(id),
                              sale_price NUMERIC(12,2) NOT NULL,
                              auction_id INT REFERENCES auctions(id),
                              listing_id INT REFERENCES market_listings(id),
                              completed_at TIMESTAMP DEFAULT NOW()
);

-- Предзаполняем роли
INSERT INTO roles (name) VALUES ('user'), ('moderator');

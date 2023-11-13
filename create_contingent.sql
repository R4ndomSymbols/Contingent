CREATE TABLE educational_program (

    id SERIAL PRIMARY KEY, 
    fgos_id VARCHAR(10) NOT NULL,
    fgos_name VARCHAR(200) NOT NULL,
    speciality_name VARCHAR(200) NOT NULL,
    course_count INT NOT NULL, --число курсов на специальности
    speciality_out_education_level INT NOT NULL, -- уровень образования на выходе
    speciality_in_education_level INT NOT NULL, -- уровень образования на входе
    knowledge_depth INT NOT NULL, -- степень углубленности программы
    group_prefix VARCHAR(10) DEFAULT NULL, 
    group_postfix VARCHAR(10) DEFAULT NULL
);

CREATE TABLE educational_group (
    
    id SERIAL PRIMARY KEY,
    program_id INT REFERENCES educational_program (id) NOT NULL,
    course_on INT NOT NULL, -- курс, на котором находится группа
    group_name VARCHAR(20) NOT NULL,
    type_of_financing INT NOT NULL, -- содержит неявно постфикс имени
    form_of_education INT NOT NULL,
    education_program_type INT NOT NULL, -- адаптированная или нет
    creation_year INT NOT NULL

);

CREATE TABLE federal_subjects (
    code INT PRIMARY KEY,
    subject_type INT NOT NULL,
    full_name VARCHAR (100) NOT NULL
    
);

CREATE TABLE districts (
    id SERIAL PRIMARY KEY,
    federal_subject_code INTEGER NOT NULL REFERENCES federal_subjects (code),
    district_type INTEGER NOT NULL,
    full_name VARCHAR (200) NOT NULL
);
-- поселение 
CREATE TABLE settlement_areas (
    id SERIAL PRIMARY KEY,
    district INTEGER NOT NULL REFERENCES districts (id),
    settlement_area_type INTEGER NOT NULL,
    full_name VARCHAR(200) NOT NULL
);
-- населенный пункт
CREATE TABLE settlements (
    id SERIAL PRIMARY KEY,
    settlement_area INTEGER NOT NULL REFERENCES settlement_areas (id),
    settlement_type INTEGER NOT NULL,
    full_name VARCHAR(200) NOT NULL
);

-- улица, может принадлежать станции и населенному пункту
CREATE TABLE streets (
    id SERIAL PRIMARY KEY,
    settlement INTEGER REFERENCES settlements (id),
    street_type INTEGER NOT NULL,
    full_name VARCHAR (200) NOT NULL
);
-- дома, может напрямую принадлежать станции
CREATE TABLE buildings (
    id SERIAL PRIMARY KEY,
    street INTEGER REFERENCES streets (id),
    full_name VARCHAR (50) NOT NULL
);

CREATE TABLE apartments (
    id SERIAL PRIMARY KEY,
    building INTEGER REFERENCES buildings (id),
    apartment_number VARCHAR (50) NOT NULL
);


CREATE TABLE addresses (
	"id" SERIAL PRIMARY KEY,
    building INT REFERENCES buildings (id),
    apartment INT REFERENCES apartments (id)
);

CREATE TABLE rus_citizenship (
    id SERIAL PRIMARY KEY,
    passport_number VARCHAR(4) NOT NULL,
    passport_series VARCHAR(6) NOT NULL,
    surname VARCHAR (100) NOT NULL,
    name VARCHAR(100) NOT NULL,
    patronymic VARCHAR (100),
    legal_address INT NOT NULL REFERENCES addresses (id)
);


CREATE TABLE students (

    id SERIAL PRIMARY KEY,
    snils VARCHAR(11) NOT NULL,
    inn VARCHAR(12) NOT NULL,
    actual_address INT NOT NULL REFERENCES addresses (id),
    date_of_birth DATE NOT NULL,
    rus_citizenship_id INT REFERENCES rus_citizenship (id),
    gender INT NOT NULL,

    grade_book_number VARCHAR(10) NOT NULL,
    target_education_agreement INT NOT NULL,
    gia_mark INT,
    gia_demo_exam_mark INT,
    paid_education_agreement INT NOT NULL,
    admission_score NUMERIC(5,3)


);

CREATE TABLE education_tag_history (
    student_id INT NOT NULL REFERENCES students (id),
    level_code INT NOT NULL
);

CREATE TABLE health_status (
    id SERIAL PRIMARY KEY,
    person_id INT NOT NULL REFERENCES students (id),
    initial_date DATE,
    end_date DATE,
    status_type INT NOT NULL,
    health_disorder_type INT NOT NULL
);

CREATE TABLE student_history (
    student_id INT NOT NULL REFERENCES students (id),
    status_code INT NOT NULL,
    recorded_on DATE NOT NULL
);

CREATE TABLE scholarship (
    id SERIAL PRIMARY KEY,
    student_id INT NOT NULL REFERENCES students (id),
    scholarship_type INT NOT NULL,
    initial_date DATE,
    end_date DATE
);

CREATE TABLE orders (

    id SERIAL PRIMARY KEY,
    specified_date DATE NOT NULL,
    effective_date DATE NOT NULL,
    serial_number INT NOT NULL,
    org_id VARCHAR (20) NOT NULL,
    type INT NOT NULL,
    name TEXT NOT NULL,
    "description" TEXT
);

CREATE TABLE student_flow (
    id SERIAL PRIMARY KEY,
    student_id INT NOT NULL REFERENCES students (id),
    order_id INT NOT NULL REFERENCES orders (id),
    group_id_to INT REFERENCES educational_group (id),
    recorded_on TIMESTAMP NOT NULL
);

-- логирование

CREATE TABLE students_history (
    ref_student_id INT NOT NULL,
    change_at TIMESTAMP NOT NULL,
    snils VARCHAR(11) NOT NULL,
    inn VARCHAR(12) NOT NULL,
    actual_address_ref_id INT NOT NULL, 
    date_of_birth DATE NOT NULL,
    rus_citizenship_ref_id INT NOT NULL,
    gender INT NOT NULL,
    grade_book_number VARCHAR(10) NOT NULL,
    target_education_agreement INT NOT NULL,
    gia_mark INT,
    gia_demo_exam_mark INT,
    paid_education_agreement INT NOT NULL
);
CREATE TABLE rus_citizenship_history (
    ref_ rus_citizenship_id INT NOT NULL,
    change_at TIMESTAMP NOT NULL,
    passport_number VARCHAR(4) NOT NULL,
    passport_series VARCHAR(6) NOT NULL,
    surname VARCHAR (100) NOT NULL,
    name VARCHAR(100) NOT NULL,
    patronymic VARCHAR (100),
    legal_address INT NOT NULL
);
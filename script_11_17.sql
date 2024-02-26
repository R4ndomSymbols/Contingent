BEGIN;

CREATE TABLE public.address_hierarchy (
    address_part_id SERIAL PRIMARY KEY,
    parent_id INT,
    address_level INT NOT NULL,
    toponym_type INT NOT NULL,
    address_name TEXT NOT NULL
);


CREATE TABLE IF NOT EXISTS public.education_tag_history
(
    student_id integer NOT NULL,
    level_code integer NOT NULL
);

CREATE TABLE IF NOT EXISTS public.educational_group
(
    group_id SERIAL NOT NULL,
    program_id integer NOT NULL,
    course_on integer NOT NULL,
    group_name text COLLATE pg_catalog."default" NOT NULL,
    type_of_financing integer NOT NULL,
    form_of_education integer NOT NULL,
    education_program_type integer NOT NULL,
    creation_year integer NOT NULL,
    letter text COLLATE pg_catalog."default",
    name_generated boolean NOT NULL,
    group_sequence_id integer NOT NULL,
    CONSTRAINT educational_group_pkey PRIMARY KEY (group_id)
);

CREATE TABLE IF NOT EXISTS public.educational_program
(
    id SERIAL NOT NULL,
    fgos_code text COLLATE pg_catalog."default" NOT NULL,
    fgos_name text COLLATE pg_catalog."default" NOT NULL,
    qualification text COLLATE pg_catalog."default" NOT NULL,
    course_count integer NOT NULL,
    speciality_out_education_level integer NOT NULL,
    speciality_in_education_level integer NOT NULL,
    knowledge_depth integer NOT NULL,
    group_prefix character varying(10) COLLATE pg_catalog."default" DEFAULT NULL::character varying,
    group_postfix character varying(10) COLLATE pg_catalog."default" DEFAULT NULL::character varying,
    training_program_type integer NOT NULL DEFAULT 0,
    CONSTRAINT educational_program_pkey PRIMARY KEY (id)
);


CREATE TABLE IF NOT EXISTS public.health_status
(
    id SERIAL NOT NULL,
    person_id integer NOT NULL,
    initial_date date,
    end_date date,
    status_type integer NOT NULL,
    health_disorder_type integer NOT NULL,
    CONSTRAINT health_status_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.orders
(
    id SERIAL NOT NULL,
    specified_date date NOT NULL,
    effective_date date NOT NULL,
    serial_number integer NOT NULL,
    org_id text COLLATE pg_catalog."default" NOT NULL,
    type integer NOT NULL,
    name text COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    is_closed boolean DEFAULT false,
    CONSTRAINT orders_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.rus_citizenship
(
    id SERIAL NOT NULL,
    passport_number text COLLATE pg_catalog."default" NOT NULL,
    passport_series text COLLATE pg_catalog."default" NOT NULL,
    surname text COLLATE pg_catalog."default" NOT NULL,
    name text COLLATE pg_catalog."default" NOT NULL,
    patronymic text COLLATE pg_catalog."default",
    legal_address integer NOT NULL,
    CONSTRAINT rus_citizenship_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.scholarship
(
    id SERIAL NOT NULL,
    student_id integer NOT NULL,
    scholarship_type integer NOT NULL,
    initial_date date,
    end_date date,
    CONSTRAINT scholarship_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.student_flow
(
    id SERIAL NOT NULL,
    student_id integer NOT NULL,
    order_id integer NOT NULL,
    group_id_to integer,
    CONSTRAINT student_flow_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.student_history
(
    student_id integer NOT NULL,
    status_code integer NOT NULL,
    recorded_on date NOT NULL
);

CREATE TABLE IF NOT EXISTS public.students
(
    id SERIAL NOT NULL,
    snils text COLLATE pg_catalog."default" NOT NULL,
    inn text COLLATE pg_catalog."default" NOT NULL,
    actual_address integer NOT NULL,
    date_of_birth date NOT NULL,
    rus_citizenship_id integer,
    gender integer NOT NULL,
    grade_book_number text COLLATE pg_catalog."default" NOT NULL,
    target_education_agreement integer NOT NULL,
    gia_mark integer,
    gia_demo_exam_mark integer,
    paid_education_agreement integer NOT NULL,
    admission_score numeric(5, 3),
    displayed_name text COLLATE pg_catalog."default" NOT NULL DEFAULT 'Не указано'::text,
    CONSTRAINT students_pkey PRIMARY KEY (id)
);

ALTER TABLE IF EXISTS public.address_hierarchy
    ADD CONSTRAINT fk_self_parent FOREIGN KEY (parent_id) 
    REFERENCES address_hierarchy (address_part_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION; 

ALTER TABLE IF EXISTS public.education_tag_history
    ADD CONSTRAINT education_tag_history_student_id_fkey FOREIGN KEY (student_id)
    REFERENCES public.students (id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.educational_group
    ADD CONSTRAINT educational_group_program_id_fkey FOREIGN KEY (program_id)
    REFERENCES public.educational_program (id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.health_status
    ADD CONSTRAINT health_status_person_id_fkey FOREIGN KEY (person_id)
    REFERENCES public.students (id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.rus_citizenship
    ADD CONSTRAINT rus_citizenship_legal_address_fkey FOREIGN KEY (legal_address)
    REFERENCES public.address_hierarchy (address_part_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.scholarship
    ADD CONSTRAINT scholarship_student_id_fkey FOREIGN KEY (student_id)
    REFERENCES public.students (id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;

ALTER TABLE IF EXISTS public.student_flow
    ADD CONSTRAINT student_flow_group_id_to_fkey FOREIGN KEY (group_id_to)
    REFERENCES public.educational_group (group_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.student_flow
    ADD CONSTRAINT student_flow_order_id_fkey FOREIGN KEY (order_id)
    REFERENCES public.orders (id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.student_flow
    ADD CONSTRAINT student_flow_student_id_fkey FOREIGN KEY (student_id)
    REFERENCES public.students (id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.student_history
    ADD CONSTRAINT student_history_student_id_fkey FOREIGN KEY (student_id)
    REFERENCES public.students (id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.students
    ADD CONSTRAINT students_actual_address_fkey FOREIGN KEY (actual_address)
    REFERENCES public.address_hierarchy (address_part_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;


ALTER TABLE IF EXISTS public.students
    ADD CONSTRAINT students_rus_citizenship_id_fkey FOREIGN KEY (rus_citizenship_id)
    REFERENCES public.rus_citizenship (id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION;

END;
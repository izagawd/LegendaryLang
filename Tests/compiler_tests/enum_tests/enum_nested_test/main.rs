fn main() -> i32 {
    enum Status {
        Ok(i32),
        Err
    }
    let s = Status::Ok(10);
    match s {
        Status::Ok(v) => v,
        Status::Err => 0
    }
}

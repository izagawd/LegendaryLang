fn main() -> i32 {
    let dd: i32 = 10;
    let rp: *mut i32 = &raw mut dd;
    *rp = 42;
    dd
}

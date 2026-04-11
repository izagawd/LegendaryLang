fn main() -> i32 {
    let dd: i32 = 42;
    let rp: *mut i32 = &raw mut dd;
    *rp
}

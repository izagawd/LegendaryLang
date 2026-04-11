fn main() -> i32 {
    let dd: i32 = 42;
    let r: &mut i32 = &mut dd;
    let rp: *mut i32 = &raw mut *r;
    *rp
}

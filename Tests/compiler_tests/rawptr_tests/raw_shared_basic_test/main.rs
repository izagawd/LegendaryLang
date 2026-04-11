fn main() -> i32 {
    let dd: i32 = 42;
    let rp: *shared i32 = &raw dd;
    *rp
}

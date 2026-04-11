fn main() -> i32 {
    let dd: i32 = 42;
    let r: &i32 = &dd;
    let rp: *shared i32 = &raw *r;
    *rp
}

fn main() -> i32 {
    let a = 3;
    let r = &mut a;
    *r = *r * *r;
    a
}

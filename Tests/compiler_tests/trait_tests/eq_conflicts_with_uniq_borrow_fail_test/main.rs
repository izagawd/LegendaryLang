fn main() -> i32 {
    let a = 5;
    let u = &mut a;
    let eq = a == 5;
    *u
}

fn main() -> i32 {
    let x = 5;
    let r = &mut x;
    x + *r
}

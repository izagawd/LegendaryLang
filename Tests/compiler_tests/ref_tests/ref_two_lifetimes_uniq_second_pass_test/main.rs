fn pick['a, 'b](x: &'a i32, y: &'b i32) -> &'a i32 {
    x
}
fn main() -> i32 {
    let a = 10;
    let b = 20;
    let r = pick(&a, &b);
    let u = &mut b;
    *r + *u
}

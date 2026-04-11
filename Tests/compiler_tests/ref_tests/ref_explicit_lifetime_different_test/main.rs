fn first['a, 'b](dd: &'a i32, kk: &'b i32) -> &'a i32 {
    dd
}
fn main() -> i32 {
    let x = 10;
    let y = 20;
    let r = first(&x, &y);
    &mut y;
    *r
}

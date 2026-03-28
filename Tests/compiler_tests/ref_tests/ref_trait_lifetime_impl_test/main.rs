trait Picker {
    fn pick<'a>(dd: &'a i32, kk: &i32) -> &'a i32;
}
impl Picker for i32 {
    fn pick<'a>(dd: &'a i32, kk: &i32) -> &'a i32 {
        dd
    }
}
fn main() -> i32 {
    let x = 10;
    let y = 20;
    let r = <i32 as Picker>::pick(&x, &y);
    *r
}

trait Doubler {
    fn double(x: Self) -> <Self as Add<Self>>::Output;
}
impl Doubler for i32 {
    fn double(x: i32) -> <i32 as Add<i32>>::Output {
        x + x
    }
}
fn main() -> i32 {
    <i32 as Doubler>::double(5)
}

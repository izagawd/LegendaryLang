fn Idk<T: Add<T, Output = T> + Sub<T, Output = T> + Copy>(one: T, two: T) -> T{
    one + two - two - two
    }
fn main() -> i32{
    Idk(2,2)
}
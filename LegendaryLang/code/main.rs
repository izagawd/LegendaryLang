fn Idk<T: Add<T> + Sub<T> + Copy>(one: T, two: T) -> T{
    one + two - two - two
    }
fn main() -> i32{
    Idk(2,2)
}
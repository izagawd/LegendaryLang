fn add_them<T: Add<T, Output = T> + Copy>(one: T, two: T) -> T {
    one + two + two
}

fn main() -> i32 {
    add_them(2, 3)
}

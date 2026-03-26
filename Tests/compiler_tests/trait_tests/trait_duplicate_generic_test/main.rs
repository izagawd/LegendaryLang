fn something<T, T>() -> i32 {
    5
}

fn main() -> i32 {
    something::<i32, i32>()
}

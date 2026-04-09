fn main() -> i32 {
    let b = Box(i32).New(42);
    let leaked: &uniq i32 = Box(i32).Leak(b);
    *leaked
}

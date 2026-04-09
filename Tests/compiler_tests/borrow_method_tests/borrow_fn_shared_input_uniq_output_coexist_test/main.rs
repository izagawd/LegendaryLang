fn shared_to_uniq(input: &i32) -> &uniq i32 {
    let b = Box(i32).New(*input);
    Box(i32).Leak(b)
}

fn main() -> i32 {
    let x = 21;
    let r: &uniq i32 = shared_to_uniq(&x);
    let s: &i32 = &x;
    *r + *s
}

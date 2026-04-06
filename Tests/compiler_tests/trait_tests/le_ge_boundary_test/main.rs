fn clamp(val: i32, low: i32, high: i32) -> i32 {
    if val <= low {
        low
    } else {
        if val >= high { high } else { val }
    }
}

fn main() -> i32 {
    clamp(0, 0, 100) + clamp(100, 0, 100) + clamp(50, 0, 100)
}

use Std.Ops.PartialOrd;
use Std.Ops.PartialEq;

fn clamp[T:! PartialOrd(T) + Copy](val: T, low: T, high: T) -> T {
    if val < low {
        low
    } else {
        if val > high { high } else { val }
    }
}

fn main() -> i32 {
    clamp(50, 0, 100) + clamp(200, 0, 100) + clamp(0 - 5, 0, 100)
}

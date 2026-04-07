use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let r1 = TryCastPrimitive(u8, 0);
    let r2 = TryCastPrimitive(u8, 255);
    let r3 = TryCastPrimitive(u8, 256);
    let result = 0;
    match r1 { Option.Some(v) => { result = result + 1; }, Option.None => {} };
    match r2 { Option.Some(v) => { result = result + 10; }, Option.None => {} };
    match r3 { Option.Some(v) => {}, Option.None => { result = result + 100; } };
    result
}
